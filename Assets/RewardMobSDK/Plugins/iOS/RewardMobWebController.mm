//
//  RewardMobWebController.m
//  UnityNativePlugin
//
//  Created by Alex Saunders on 2017-09-22.
//  Copyright © 2017 Alex Saunders. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "RewardMobWebController.h"

@implementation RewardMobWebController {
    SFSafariViewController *svc;
}

-(void)openRewardsFor:(NSString *) urlStr {
    // Do some URL parsing.
    NSURL *openRewardsURL = [NSURL URLWithString: urlStr];
    
    // Get the URL components to extract the app id.
    NSURLComponents *urlComponents = [NSURLComponents componentsWithURL:openRewardsURL resolvingAgainstBaseURL:NO];
    NSArray *queryItems = urlComponents.queryItems;
    
    // Find the query item with name 'app_id' and assign it to the appId variable.
    NSString *appId = @"";
    for(NSURLQueryItem *item in queryItems) {
        if([item.name isEqualToString:@"app_id"]) {
            appId = item.value;
        }
    }
    
    // Check if the App is installed and if so, open via custom URI scheme & return.
    if([[UIApplication sharedApplication] canOpenURL:[NSURL URLWithString:@"rewardmob://"]]) {
        // Open via custom URI scheme.
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:[NSString stringWithFormat:@"rewardmob://open-rewards?app_id=%@", appId]]];
        return;
    } else {
        // The RewardMob app is not installed.
        // Open system browser to the Download Rewardmob link.
        NSString *downloadRewardMobLink = @"https://rewardmob.com/download-rewardmob";
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:downloadRewardMobLink] options:[NSDictionary dictionary] completionHandler:nil];
        return;
    }
}

-(void)openSafariViewControllerFor:(NSString *) urlStr {
    
    // Check if the App is installed and if so, open the URL & return.
    if([[UIApplication sharedApplication] canOpenURL:[NSURL URLWithString:@"rewardmob://"]]) {
        [[UIApplication sharedApplication] openURL:[NSURL URLWithString:[NSString stringWithFormat:@"rewardmob://auth/oauth/authorize?%@", [[urlStr componentsSeparatedByString:@"?"] objectAtIndex: 1]]] options:[NSDictionary dictionary] completionHandler:nil];
        return;
    }
    
    // (Re)create the SafariViewController
    NSURL *url = [NSURL URLWithString:urlStr];
    svc = [[SFSafariViewController alloc] initWithURL:url];
    
    // Presentation & Transition Styles
    svc.modalPresentationStyle = UIModalPresentationOverFullScreen;
    svc.modalTransitionStyle = UIModalTransitionStyleCoverVertical;
    
    // Get Unity's Root ViewController & Presnt svc
    UIViewController *rootVC = [UIApplication sharedApplication].keyWindow.rootViewController;
    [rootVC presentViewController:svc animated:YES completion:nil];
}

-(void)hideSafariViewController {
    // If set, dismiss the SafariViewController.
    if(svc) {
        [svc dismissViewControllerAnimated:YES completion:^{
            NSLog(@"Hid the SafariViewController via hideSafariViewController()");
        }];
    }
}

-(void)safariViewController:(SFSafariViewController *)controller didCompleteInitialLoad:(BOOL)didLoadSuccessfully {
    /*
     Delegate is only assigned when logging out user.
     After the initial load is complete (user logout),
     then dismiss the VC. */
    [svc dismissViewControllerAnimated:YES completion:nil];
}

-(void)logoutUserForMode:(NSString *)mode {
    
    if([[UIApplication sharedApplication] canOpenURL:[NSURL URLWithString:@"rewardmob://"]]) {
        // Authentication happens within the RewardMob app, so don't
        // bother triggering the webview in this case.
        return;
    }
    
    // (Re)create the SafariViewController
    NSString *urlStr = @"https://rewardmob.com/auth/logout";
    if([mode isEqualToString:@"dev"]) {
        urlStr = @"https://dev.rewardmob.com/auth/logout";
    }
    
    NSURL *url = [NSURL URLWithString:urlStr];
    svc = [[SFSafariViewController alloc] initWithURL:url];
    svc.delegate = self; // Asign delegate as self to know when initial load is complete.
    
    // Presentation & Transition Styles
    svc.modalPresentationStyle = UIModalPresentationOverFullScreen;
    svc.modalTransitionStyle = UIModalTransitionStyleCoverVertical;
    
    // Get Unity's Root ViewController & Presnt svc
    UIViewController *rootVC = [UIApplication sharedApplication].keyWindow.rootViewController;
    [rootVC presentViewController:svc animated:YES completion:nil];
}
@end

// Converts C style string to NSString
NSString* CreateNSString (const char* string)
{
    if (string)
        return [NSString stringWithUTF8String: string];
    else
        return [NSString stringWithUTF8String: ""];
}

RewardMobWebController *rmwc = nil;

extern "C" {
    void _OpenSafari (const char* url) {
        if(rmwc == nil) {
            rmwc = [[RewardMobWebController alloc] init];
        }
        
        [rmwc openSafariViewControllerFor:CreateNSString(url)];
    }
    
    void _CloseSafari () {
        if(rmwc == nil) {
            rmwc = [[RewardMobWebController alloc] init];
        }
        
        [rmwc hideSafariViewController];
    }
    
    void _LogoutUser(const char* mode) {
        if(rmwc == nil) {
            rmwc = [[RewardMobWebController alloc] init];
        }
        
        [rmwc logoutUserForMode:CreateNSString(mode)];
    }
    
    void _OpenRewards (const char* url) {
        if(rmwc == nil) {
            rmwc = [[RewardMobWebController alloc] init];
        }
        
        [rmwc openRewardsFor:CreateNSString(url)];
    }
}

