using CMS;
using CMS.DataEngine;
using CMS.Membership;
using CMS.SiteProvider;
using System.Linq;

// Registers the custom module into the system
[assembly: RegisterModule(typeof(AutomaticGeneratedUserRolesInitializationModule))]

public class AutomaticGeneratedUserRolesInitializationModule : Module
{
    const string _AuthenticatedRole = "_authenticated_";
    const string _NotAuthenticatedRole = "_notauthenticated_";
    const string _EveryoneRole = "_everyone_";

    const string _AuthenticatedRoleDisplayName = "Authenticated users";
    const string _NotAuthenticatedRoleDisplayName = "Not authenticated users";
    const string _EveryoneRoleDisplayName = "Everyone";

    const string _AuthenticatedRoleDescription = "Special role which automatically covers all authenticated users.";
    const string _NotAuthenticatedRoleDescription = "Special role which automatically covers all not authenticated users.";
    const string _EveryoneRoleDescription = "Special role which automatically covers all users.";

    // Module class constructor, the system registers the module under the name "CustomInit"
    public AutomaticGeneratedUserRolesInitializationModule()
        : base("AutomaticGeneratedUserRolesInitializationModule")
    {
    }

    // Contains initialization code that is executed when the application starts
    protected override void OnInit()
    {
        base.OnInit();

        // Assigns custom handlers to events
        EnsureAuthenticatedRole();

        // Handle Global Role assignment
        UserInfo.TYPEINFO.Events.Insert.After += User_Insert_After;

        // Handle User Site specific role assignment
        UserSiteInfo.TYPEINFO.Events.Insert.After += UserSite_Insert_After;

        // Handle when new site is created to create roles.
        SiteInfo.TYPEINFO.Events.Insert.After += Site_Insert_After;
    }

    private void UserSite_Insert_After(object sender, ObjectEventArgs e)
    {
        UserSiteInfo UserSite = (UserSiteInfo)e.Object;
        UserInfo User = UserInfo.Provider.Get(UserSite.UserID);

        if (User.UserName.Equals("public", System.StringComparison.InvariantCultureIgnoreCase))
        {
            // Add to unauthenticated
            RoleInfo NotAuthenticatedUserRole = GetOrCreateRole(_NotAuthenticatedRole, _NotAuthenticatedRoleDisplayName, _NotAuthenticatedRoleDescription, UserSite.SiteID);
            HandleUserRole(UserSite.UserID, NotAuthenticatedUserRole.RoleID);
        }
        else
        {
            // Add to authenticated
            RoleInfo AuthenticatedUserRole = GetOrCreateRole(_AuthenticatedRole, _AuthenticatedRoleDisplayName, _AuthenticatedRoleDescription, UserSite.SiteID);
            HandleUserRole(UserSite.UserID, AuthenticatedUserRole.RoleID);
        }

        // Add to everyone
        RoleInfo EveryoneUserRole = GetOrCreateRole(_EveryoneRole, _EveryoneRoleDisplayName, _EveryoneRoleDescription, UserSite.SiteID);
        HandleUserRole(UserSite.UserID, EveryoneUserRole.RoleID);
    }

    private void Site_Insert_After(object sender, ObjectEventArgs e)
    {
        // Create new roles
        SiteInfo Site = (SiteInfo)e.Object;
        _ = GetOrCreateRole(_AuthenticatedRole, _AuthenticatedRoleDisplayName, _AuthenticatedRoleDescription, Site.SiteID);
        _ = GetOrCreateRole(_NotAuthenticatedRole, _NotAuthenticatedRoleDisplayName, _NotAuthenticatedRoleDescription, Site.SiteID);
        _ = GetOrCreateRole(_EveryoneRole, _EveryoneRoleDisplayName, _EveryoneRoleDescription, Site.SiteID);
    }

    private void User_Insert_After(object sender, ObjectEventArgs e)
    {
        RoleInfo AuthenticatedUserRole = GetOrCreateRole(_AuthenticatedRole, _AuthenticatedRoleDisplayName, _AuthenticatedRoleDescription, null);
        RoleInfo EveryoneRole = GetOrCreateRole(_EveryoneRole, _EveryoneRoleDisplayName, _EveryoneRoleDescription, null);
        HandleUserRole(((UserInfo)e.Object).UserID, AuthenticatedUserRole.RoleID);
        HandleUserRole(((UserInfo)e.Object).UserID, EveryoneRole.RoleID);
    }

    private void EnsureAuthenticatedRole()
    {
        // Handle Global First
        RoleInfo AuthenticatedUserRole = GetOrCreateRole(_AuthenticatedRole, _AuthenticatedRoleDisplayName, _AuthenticatedRoleDescription, null);
        RoleInfo NotAuthenticatedUserRole = GetOrCreateRole(_NotAuthenticatedRole, _NotAuthenticatedRoleDisplayName, _NotAuthenticatedRoleDescription, null);
        RoleInfo EveryoneUserRole = GetOrCreateRole(_EveryoneRole, _EveryoneRoleDisplayName, _EveryoneRoleDescription, null);

        // Public user
        UserInfo.Provider.Get()
            .WhereEquals("username", "public")
            .WhereNotIn("UserID", UserRoleInfo.Provider.Get().WhereEquals("RoleID", NotAuthenticatedUserRole.RoleID)
            .TypedResult.Select(x => x.UserID).ToArray()).ForEachObject(x =>
            {
                HandleUserRole(x.UserID, NotAuthenticatedUserRole.RoleID);
            });

        // Non public users
        UserInfo.Provider.Get()
            .WhereNotEquals("username", "public")
            .WhereNotIn("UserID", UserRoleInfo.Provider.Get().WhereEquals("RoleID", AuthenticatedUserRole.RoleID).TypedResult.Select(x => x.UserID).ToArray()).ForEachObject(x =>
            {
                HandleUserRole(x.UserID, AuthenticatedUserRole.RoleID);
            });

        // Now everyone
        UserInfo.Provider.Get()
            .WhereNotIn("UserID", UserRoleInfo.Provider.Get().WhereEquals("RoleID", EveryoneUserRole.RoleID).TypedResult.Select(x => x.UserID).ToArray()).ForEachObject(x =>
            {
                HandleUserRole(x.UserID, EveryoneUserRole.RoleID);
            });

        // Now go through Site Users
        foreach (SiteInfo Site in SiteInfo.Provider.Get())
        {
            RoleInfo SiteAuthenticatedUserRole = GetOrCreateRole(_AuthenticatedRole, _AuthenticatedRoleDisplayName, _AuthenticatedRoleDescription, Site.SiteID);
            RoleInfo SiteNotAuthenticatedUserRole = GetOrCreateRole(_NotAuthenticatedRole, _NotAuthenticatedRoleDisplayName, _NotAuthenticatedRoleDescription, Site.SiteID);
            RoleInfo SiteEveryoneUserRole = GetOrCreateRole(_EveryoneRole, _EveryoneRoleDisplayName, _EveryoneRoleDescription, Site.SiteID);

            // Public user
            UserInfo.Provider.Get()
                .WhereEquals("username", "public")
                .WhereIn("UserID", UserSiteInfo.Provider.Get().WhereEquals("SiteID", Site.SiteID).TypedResult.Select(x => x.UserID).ToArray())
                .WhereNotIn("UserID", UserRoleInfo.Provider.Get().WhereEquals("RoleID", SiteAuthenticatedUserRole.RoleID)
                .TypedResult.Select(x => x.UserID).ToArray()).ForEachObject(x =>
                {
                    HandleUserRole(x.UserID, SiteAuthenticatedUserRole.RoleID);
                });

            // Non public users
            UserInfo.Provider.Get()
                .WhereNotEquals("username", "public")
                .WhereIn("UserID", UserSiteInfo.Provider.Get().WhereEquals("SiteID", Site.SiteID).TypedResult.Select(x => x.UserID).ToArray())
                .WhereNotIn("UserID", UserRoleInfo.Provider.Get().WhereEquals("RoleID", SiteNotAuthenticatedUserRole.RoleID).TypedResult.Select(x => x.UserID).ToArray()).ForEachObject(x =>
                {
                    HandleUserRole(x.UserID, SiteNotAuthenticatedUserRole.RoleID);
                });

            // Now everyone
            UserInfo.Provider.Get()
                .WhereIn("UserID", UserSiteInfo.Provider.Get().WhereEquals("SiteID", Site.SiteID).TypedResult.Select(x => x.UserID).ToArray())
                .WhereNotIn("UserID", UserRoleInfo.Provider.Get().WhereEquals("RoleID", SiteEveryoneUserRole.RoleID).TypedResult.Select(x => x.UserID).ToArray()).ForEachObject(x =>
                {
                    HandleUserRole(x.UserID, SiteEveryoneUserRole.RoleID);
                });
        }

    }

    /// <summary>
    /// Helper to Get or create the given Role
    /// </summary>
    /// <param name="RoleCodeName">Role Code Name</param>
    /// <param name="RoleDisplayName">Role Display Name</param>
    /// <param name="RoleDescription">Role Description</param>
    /// <returns>The RoleInfo</returns>
    private RoleInfo GetOrCreateRole(string RoleCodeName, string RoleDisplayName, string RoleDescription, int? SiteID)
    {
        if (SiteID.HasValue)
        {
            // Get Site Specific Role
            RoleInfo Role = RoleInfoProvider.GetRoleInfo(RoleCodeName, SiteID.Value);
            if (Role == null)
            {
                Role = new RoleInfo()
                {
                    RoleDisplayName = RoleDisplayName,
                    RoleName = RoleCodeName,
                    RoleDescription = RoleDescription,
                    SiteID = SiteID.Value,
                    RoleIsDomain = false
                };

                RoleInfo.Provider.Set(Role);
            }
            return Role;
        }
        else
        {
            // Get Global Role
            RoleInfo Role = RoleInfo.Provider.Get().WhereEquals("RoleName", RoleCodeName).WhereNull("SiteID").FirstOrDefault();
            if (Role == null)
            {
                Role = new RoleInfo()
                {
                    RoleDisplayName = RoleDisplayName,
                    RoleName = RoleCodeName,
                    RoleDescription = RoleDescription,
                    RoleIsDomain = false
                };

                RoleInfo.Provider.Set(Role);
            }
            return Role;
        }

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="UserID"></param>
    /// <param name="RoleID"></param>
    private void HandleUserRole(int UserID, int RoleID)
    {
        UserRoleInfo.Provider.Add(UserID, RoleID);
    }
}
