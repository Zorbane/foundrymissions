﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace FoundryMissionsCom
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "ContactUs",
                "contact-us",
                new { controller = "home", action = "contact-us" });

            routes.MapRoute(
                "RandomMission",
                "missions/random",
                new { controller = "Missions", action = "Random" }
            );

            routes.MapRoute(
                "SubmitMission",
                "missions/submit",
                new { controller = "Missions", action = "Submit" }
            );

            routes.MapRoute(
                "SearchMission",
                "missions/search",
                new { controller = "Missions", action = "Search" }
            );

            routes.MapRoute(
                "AdvancedSearch",
                "missions/advanced-search",
                new { controller = "Missions", action = "Advanced-Search" }
            );

            routes.MapRoute(
                "SearchResults",
                "missions/searchresults",
                new { controller = "Missions", action = "SearchResults" }
            );

            routes.MapRoute(
                "AuthorPage",
                "missions/author",
                new { controller = "Missions", action = "author", author= "{author}" }
            );

            routes.MapRoute(
                "EditMissionLink",
                "missions/edit/{link}",
                new { controller = "Missions", action = "Edit", link = "{link}" }
            );

            routes.MapRoute(
                "MissionLink",
                "missions/{link}",
                new { controller = "Missions", action = "Details" }
            );

            routes.MapRoute(
                "MissionIndex",
                "missions/",
                new { controller = "Missions", action = "index" }
            );

            routes.MapRoute(
                "CollectionIndex",
                "collections",
                new { controller = "collections", action = "index" }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
