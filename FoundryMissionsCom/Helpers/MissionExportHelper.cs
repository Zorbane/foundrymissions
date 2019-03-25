﻿using FoundryMissionsCom.Models;
using FoundryMissionsCom.Models.FoundryMissionModels;
using FoundryMissionsCom.Models.FoundryMissionModels.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FoundryMissionsCom.Helpers
{
    public static class MissionExportHelper
    {
        private const string ExportKlingon = "Allegiance_Klingon";
        private const string ExportFederation = "Allegiance_Starfleet";

        public static Mission ParseExportToMission(ApplicationDbContext context, string exportText)
        {
            //this gets a foundry export mission type, need to convert it to our mission
            var exportmission = StarbaseUGC.Foundry.Engine.Serializers.FoundryMissionSerializer.ParseMissionText(exportText);
            var mission = new Mission();

            mission.AuthorUserId = exportmission.Project.AccountName; //we don't actually use this field but can use it to find user match
            mission.Description = exportmission.Project.Description;
            mission.Faction = GetFactionFromExportFaction(exportmission.Project.RestrictionProperties.Faction);
            mission.MinimumLevel = string.IsNullOrWhiteSpace(exportmission.Project.RestrictionProperties.MinLevel) ? 1 : Convert.ToInt32(exportmission.Project.RestrictionProperties.MinLevel);
            mission.MissionExportText = exportText;
            mission.Name = GetMissionName(exportmission.Project.PublicName);

            //try and match the userid to an actual user, if it exists use that
            var user = context.Users.Where(u => u.UserName.Equals(exportmission.Project.AccountName)).FirstOrDefault();

            if (user != null)
            {
                mission.Author = user;
            }
            
            return mission;

        }

        private static Random random = new Random();
        public static string GenerateRandomID()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, 9)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private static string GetMissionName(string exportMissionName)
        {
            //remove the first and last character because they are "asdfasdr"
            var name = exportMissionName.Substring(1);
            name = name.Substring(0, name.Length - 1);
            return name;

        }

        private static Faction GetFactionFromExportFaction(string exportFaction)
        {
            switch(exportFaction)
            {
                case ExportFederation:
                    return Faction.Federation;
                case ExportKlingon:
                    return Faction.Klingon;
                default:
                    return Faction.Federation;

            }
        }
    }
}