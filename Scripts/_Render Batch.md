# Render Batch - Read Me

|||
|---|---|
|Version|0.7|
|Updated|2024/11/08|


## Instructions

|Feature|Description|
|---|---|
|Base File Name|Deteremines the path to the file|
|All Templates|Check one or more templates to render as. All checked templates will be rendered for all regions|
|Shorts Templates|Check only 1 for the template that will be used for shorts. If multiple are selected, then only the first one detected will be used.|
|Render Shorts|Determines whether Regions with #short in the name will be rendered with the Shorts format|
|Shorts Max Length|Determines that maximum length to be considered a short|
|Serialized Out Files|Will serialize files by adding a number to the start of the filename. |
|Render Project| Renders the whole project|
|Render Selection| Renders only the selection|
|Render Regions| (Default) Renders regions|

## Feature Requests

*   UI Clean Up
*   Handle Error when there's not file selected


## Version History


### Version 0.7

*   New Features
*   Fixes
    *   Duplicate files when Shorts are rendered
*   Bugs Found
    *   Duplicate files when Shorts are rendered
        1. Create 2+ regions with at least 1 having #short
        1. Open Render Batch script
        1. Select 1+ non-short template
        1. Select 1+ Short template
        1. Start the rendering (Click OK)


### Version 0.6

*   New Features
    *   Ability to select the Shorts template
*   Fixes
    *   Duplicate files when Shorts are rendered
*   Bugs Found


### Version 0.5 

*   New Features
*   Fixes
    *   Fixed the Render Shorts checkbox 
*   Bugs Found


### Version 0.4 

*   New Features
    *   Added ability to serialize files output during render
*   Fixes
*   Bugs Found
    *   The checkbox for rendering shorts does not determine whether #shorts will render

    
### Version 0.3 

*   New Features
    *   Added the ability to set a maximum length for files with #short
*   Fixes
*   Bugs Found

    
### Version 0.2

*   New Features
    *   Added the ability to use #Short in a region name to autoformat into a Youtube Shorts format
*   Fixes
*   Bugs Found
    
    
### Version 0.1

*   New Features
    *   Added git hooks for automation
    *   Updated .gitignore
*   Fixes
*   Bugs Found
    
    
### Version 0.0

*   New Features
    *   Added a suffix based on the template when multiple templates are selected
*   Fixes
*   Bugs Found


