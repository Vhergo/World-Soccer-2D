#import "UnityAppController.h"

@interface OverrideOpenURL : UnityAppController
@end


IMPL_APP_CONTROLLER_SUBCLASS(OverrideOpenURL)


@implementation OverrideOpenURL

- (BOOL)application:(UIApplication*)application openURL:(NSURL*)url sourceApplication:(NSString*)sourceApplication annotation:(id)annotation
{
    NSMutableArray* keys    = [NSMutableArray arrayWithCapacity: 3];
    NSMutableArray* values  = [NSMutableArray arrayWithCapacity: 3];
    
#define ADD_ITEM(item)  do{ if(item) {[keys addObject:@#item]; [values addObject:item];} }while(0)
    
    ADD_ITEM(url);
    ADD_ITEM(sourceApplication);
    ADD_ITEM(annotation);
    
#undef ADD_ITEM
    NSLog(@"URL is %@", url);
    NSLog(@"Scheme is %@", [url scheme]);
    if([[url scheme] hasPrefix:@"rm"]) {
        const char* object = "RewardMobManager";
        
        NSString *URLString = [url absoluteString];
        NSString *queryParams = [url query];
        
        // Check Query Params for Environment
        if(queryParams != nil) {
            NSURLComponents *comps = [NSURLComponents componentsWithURL:url resolvingAgainstBaseURL:NO];
            NSLog(@"URL Components %@", comps.queryItems);
            
            for(NSURLQueryItem *item in comps.queryItems) {
                if([item.name isEqualToString:@"env"]) {
                    // We have been given an environment.
                    NSString *env = @"prod";
                    if([item.value isEqualToString:@"dev"]) {
                        env = @"dev";
                    }
                    
                    const char* method = "SetEnvironment";
                    
                    UnitySendMessage(object, method, [env UTF8String]);
                    break;
                }
            }
        }
        
        // Check URL Fragment for Authorization
        // Refactor below code to use [url fragment]
        NSLog(@"URL Fragment is %@", [url fragment]);
        
        //----------
        
        // Check for Authorization URL Fragment
        if([url fragment] != nil) {
            // If we got a URL fragment, it's an access token.
            const char* method = "OnAccessToken";
            UnitySendMessage(object, method, [[url fragment] UTF8String]);
        }
    }
    
    NSDictionary* notifData = [NSDictionary dictionaryWithObjects: values forKeys: keys];
    //AppController_SendNotificationWithArg(kOn, notifData);
    return YES;
}
@end

