#import <Foundation/Foundation.h>

#if __has_include(<AppTrackingTransparency/AppTrackingTransparency.h>)
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#endif

extern "C" void UnitySendMessage(const char *obj, const char *method, const char *msg);

extern "C" void BusPuzzle_RequestTrackingAuthorization(const char *gameObjectName)
{
    NSString *targetName = [NSString stringWithUTF8String:gameObjectName ?: ""];

    void (^complete)(NSInteger) = ^(NSInteger status) {
        dispatch_async(dispatch_get_main_queue(), ^{
            NSString *statusString = [NSString stringWithFormat:@"%ld", (long)status];
            UnitySendMessage([targetName UTF8String], "HandleTrackingAuthorizationCompleted", [statusString UTF8String]);
        });
    };

#if __has_include(<AppTrackingTransparency/AppTrackingTransparency.h>)
    if (@available(iOS 14, *)) {
        [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
            complete((NSInteger)status);
        }];
    } else {
        complete(0);
    }
#else
    complete(0);
#endif
}
