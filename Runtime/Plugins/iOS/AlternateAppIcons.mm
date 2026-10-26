// Native side of the iOS platform of Alternate App Icons (IosIconPlatform.cs). Written to build with and without ARC.
#import <UIKit/UIKit.h>

typedef void (*AlternateAppIconsCompletion)(int request, const char* error);

// Copy of a string that IL2CPP frees once it has made it a managed string: NULL for nil.
static char* AlternateAppIcons_Copy(NSString* string)
{
    if (string == nil) return NULL;

    const char* utf8 = [string UTF8String];
    char* copy = (char*)malloc(strlen(utf8) + 1);
    strcpy(copy, utf8);
    return copy;
}

static void AlternateAppIcons_AddIconSets(NSMutableOrderedSet* iconSets, id icons)
{
    if (![icons isKindOfClass:[NSDictionary class]]) return;

    id alternateIcons = [(NSDictionary*)icons objectForKey:@"CFBundleAlternateIcons"];
    if (![alternateIcons isKindOfClass:[NSDictionary class]]) return;

    [iconSets addObjectsFromArray:[(NSDictionary*)alternateIcons allKeys]];
}

extern "C"
{
    int AlternateAppIcons_SupportsAlternateIcons()
    {
        return [UIApplication sharedApplication].supportsAlternateIcons ? 1 : 0;
    }

    // Icon sets of the alternate icons that Xcode listed in the Info.plist, one per line: the iPad has its own list.
    char* AlternateAppIcons_GetIconSets()
    {
        NSMutableOrderedSet* iconSets = [NSMutableOrderedSet orderedSet];
        NSDictionary* info = [NSBundle mainBundle].infoDictionary;
        AlternateAppIcons_AddIconSets(iconSets, [info objectForKey:@"CFBundleIcons"]);
        AlternateAppIcons_AddIconSets(iconSets, [info objectForKey:@"CFBundleIcons~ipad"]);
        return AlternateAppIcons_Copy([[iconSets array] componentsJoinedByString:@"\n"]);
    }

    // Icon set shown now, empty for the default icon.
    char* AlternateAppIcons_GetAlternateIconName()
    {
        NSString* iconSet = [UIApplication sharedApplication].alternateIconName;
        return AlternateAppIcons_Copy(iconSet != nil ? iconSet : @"");
    }

    // Shows an icon set, NULL for the default icon, then calls back on the main thread with NULL or the error.
    void AlternateAppIcons_SetAlternateIconName(const char* iconSet, int request, AlternateAppIconsCompletion completion)
    {
        NSString* name = iconSet != NULL && iconSet[0] != '\0' ? [NSString stringWithUTF8String:iconSet] : nil;
        [[UIApplication sharedApplication] setAlternateIconName:name completionHandler:^(NSError* error)
        {
            NSString* message = error != nil ? error.localizedDescription : nil;
            dispatch_async(dispatch_get_main_queue(), ^
            {
                completion(request, message != nil ? [message UTF8String] : NULL);
            });
        }];
    }
}
