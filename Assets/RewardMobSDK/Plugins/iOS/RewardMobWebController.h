//
//  RewardMobWebController.h
//  UnityNativePlugin
//
//  Created by Alex Saunders on 2017-09-22.
//  Copyright © 2017 Alex Saunders. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <SafariServices/SafariServices.h>

@interface RewardMobWebController: NSObject <SFSafariViewControllerDelegate>

-(void)openSafariViewControllerFor:(NSString *)url;
-(void)hideSafariViewController;
-(void)logoutUserForMode:(NSString *)mode;
-(void)openRewardsFor:(NSString *) urlStr;

@end
