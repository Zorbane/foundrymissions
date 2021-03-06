﻿using FoundryMissionsCom.Models.FoundryMissionModels;
using FoundryMissionsCom.Models.FoundryMissionModels.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace FoundryMissionsCom.Models.FoundryMissionViewModels
{
    public class AdvancedSearchViewModel
    {
        public int Id { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }

        [DisplayName("Minimum Level")]
        public int? MinimumLevel { get; set; }
        public Faction? Faction { get; set; }
        public List<string> Tags { get; set; }

        [DisplayName("Description Keywords")]
        public string Keywords { get; set; }

        public List<ListMissionViewModel> Missions { get; set; }
    }
}