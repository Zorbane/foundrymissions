﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StarbaseUGC.Foundry.Winforms.Models
{
    class Objective
    {
        public JToken Json { get; set; }

        public override string ToString()
        {
            return Json["UIString"].ToString();
        }
    }
}
