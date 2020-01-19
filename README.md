# Kentico Automatic Generated User Roles
Adds Automatic `Everyone`, `Authenticated`, and `Not Authenticated` Roles back to Kentico (removed in K12) and automatically handles assigning users to these roles.

# Installation
1. Install the `HBS.AutomaticGeneratedUserRoles.Kentico` NuGet Package to your MVC Site
1. Install the `HBS.AutomaticGeneratedUserRoles.Kentico.Mother` NuGet Package to your Kentico EMS Site

# Usage
This tool works automatically.  

Upon start up it will ensure the Roles exist for all sites (and as a global role) and ensure all users are in the appropriate roles (either Authenticated and Everyone for non `public` user, or Not Authenticated and Everyone for the `public` user)

Whenever a user is created, it will immediately add them to the global roles, and as the user is assigned to various sites, will also assign them to the roles for that site.

# Contributions, but fixes and License
Feel free to Fork and submit pull requests to contribute.

You can submit bugs through the issue list and i will get to them as soon as i can, unless you want to fix it yourself and submit a pull request!

Check the License.txt for License information

# Compatability
Can be used on any Kentico 12 SP site (hotfix 29 or above).
