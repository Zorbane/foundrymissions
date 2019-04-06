﻿using Newtonsoft.Json;
using StarbaseUGC.Foundry.Engine.Helpers;
using StarbaseUGC.Foundry.Engine.Models;
using StarbaseUGC.Foundry.Engine.Models.Components;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace StarbaseUGC.Foundry.Engine.Serializers
{
    public static class FoundryMissionSerializer
    {
        public static FoundryMission ImportMission(string fileName)
        {
            var txt = File.ReadAllText(fileName);
            return ParseMissionText(txt);
        }

        public static string ExportMissionToJson(FoundryMission mission, Formatting = Formatting.Indented)
        {
            var json = JsonConvert.SerializeObject(mission, Formatting.Indented, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                 
            });

            return json;
        }

        public static FoundryMission ParseMissionText(string txt)
        {
            var importLines = new List<string>(txt.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));

            var mission = new FoundryMission();

            mission.Namespace = importLines.Find(s => s.StartsWith(Constants.FoundryMission.NameSpace)).Replace(Constants.FoundryMission.NameSpace, string.Empty);
            mission.Project = GetProject(importLines);
            mission.Mission = GetMission(importLines);
            mission.Maps = GetMaps(importLines);
            mission.Components = GetComponents(importLines);
            //costumes

            return mission;
        }

        private static Project GetProject(List<string> importLines)
        {
            var objects = GetFoundryObjectsByName(importLines, "Project");
            //since there is only one project object we can safely return the first one

            return (Project)objects[0];
        }

        private static Mission GetMission(List<string> importLines)
        {
            var objects = GetFoundryObjectsByName(importLines, "Mission");

            //since there is only one mission object we can safely return the first one

            return (Mission)objects[0];
        }

        private static List<Map> GetMaps(List<string> importLines)
        {
            var objects = GetFoundryObjectsByName(importLines, "Map");
            var maps = objects.Cast<Map>().ToList();
            return maps;
        }

        private static List<Component> GetComponents(List<string> importLines)
        {

            var objects = new List<FoundryObject>();

            //find all the lines that match component, but only like.  include tab at the beginning and  space at the end
            var matchIndexs = importLines.Select((text, index) => text.Contains($"\t{Constants.Component.Title} ") ? index : -1).Where(index => index != -1).ToArray();

            foreach (var matchIndex in matchIndexs)
            {
                var index = matchIndex;
                var text = importLines[index];

                //make sure its actually a component line, the first word must be component
                var split = text.Split(new char[] { ' ' });

                if (split.Length < 1)
                {
                    continue;
                }

                if (!split[0].Trim().Equals(Constants.Component.Title))
                {
                    continue;
                }

                var foundryObject = GetFoundryObjectByIndex(importLines, ref index);

                objects.Add(foundryObject);
            }

            var components = objects.Cast<Component>().ToList();
            return components;
        }

        private static List<FoundryObject> GetFoundryObjectsByName(List<string> importLines, string name)
        {
            var objects = new List<FoundryObject>();

            //find all the lines that match the name
            var matchIndexs = importLines.Select((text, index) => text.Equals(name) ? index : -1).Where(index => index != -1).ToArray();

            foreach (var matchIndex in matchIndexs)
            {
                var index = matchIndex;
                var foundryObject = GetFoundryObjectByIndex(importLines, ref index);

                objects.Add(foundryObject);
            }

            return objects;
        }

        /// <summary>
        /// Get the foundry object at the specific index
        /// </summary>
        /// <param name="importLines"></param>
        /// <param name="index">by reference because other foundry objects may be found inside and the index must keep going</param>
        /// <returns></returns>
        private static FoundryObject GetFoundryObjectByIndex(List<string> importLines, ref int currentIndex, string enforcedTitle = "")
        {
            var title = enforcedTitle; //allow us to set a title
            if (string.IsNullOrWhiteSpace(enforcedTitle))
            {
                title = importLines[currentIndex].Trim(); //this is the name of the object
            }

            var foundryObject = FoundryObjectFactory.CreateFoundryObject(title, importLines, currentIndex);

            //sometimes the title has extra data in it, the first word is always the title.  need to check for this
            var split = title.Split(Constants.SplitSpace);
            if (split.Length > 1)
            {
                title = split[0];
            }

            foundryObject.Title = title;
            var openText = importLines[currentIndex + 1]; //this represents the { 
            var closeText = openText.Replace(Constants.FoundryObjectStartCharacter, Constants.FoundryObjectEndCharacter); //this represents the } but keeps the indentation so we can find the correct "end"
            currentIndex = currentIndex + 2;          //this represents the first text that is actually a field

            for (; currentIndex < importLines.Count; currentIndex++)
            {
                var text = importLines[currentIndex];

                //first check for reserved words

                #region Empty Lines
                //skip empty lines
                if (string.IsNullOrEmpty(text.Trim()))
                {
                    continue;
                }
                #endregion

                #region Hit Close Text
                //go until it reaches the close text and then break                
                if (text.Equals(closeText))
                {
                    break;
                }
                #endregion

                #region Whens
                if (text.Contains(Constants.Component.When.Title))
                {
                    //if it has a when then it might be a trigger, trim and then split by space
                    //if it has 1 or 2 strings in the array then its a when, otherwise it was just some text that said When
                    split = text.Trim().Split(new char[] { ' ' });
                    if (split.Length <= 2)
                    {
                        HandleWhen(foundryObject, importLines, ref currentIndex);
                        continue;
                    }
                }
                #endregion

                #region External Variable
                if (text.Trim().Equals(Constants.Component.ExternalVar.Title))
                {

                }
                #endregion

                #region New Foundry Object
                //if it is not then next check if it is a new foundry object
                var foundryObjectCheckIndex = currentIndex + 1;
                //but first make sure we don't go over the text size, may be impossible to happen but should check anyways
                if (foundryObjectCheckIndex > importLines.Count - 1)
                {
                    break;
                }
                var foundryObjectCheckText = importLines[foundryObjectCheckIndex];
                if (foundryObjectCheckText.Trim().StartsWith(Constants.FoundryObjectStartCharacter))
                {
                    var newFoundryObject = GetFoundryObjectByIndex(importLines, ref currentIndex);

                    foundryObject.FoundryObjects.Add(newFoundryObject);
                }
                #endregion
                #region Add new Field
                else //it is neither the end or a new foundry object that means we have a new field!
                {
                    //trim the text, the first word is the field name, everything after that is the data
                    text = text.Trim();
                    split = text.Split(' ');
                    var fieldName = split[0];
                    var fieldValue = string.Empty;
                    if (split.Length > 1)
                    {
                        fieldValue = text.Substring(fieldName.Length + 1);
                    }
                    if (!foundryObject.Fields.ContainsKey(fieldName)) // some can be in ther emultiple times like "END"
                    {
                        foundryObject.Fields.Add(fieldName, fieldValue);
                    }
                }
                #endregion
            }

            return foundryObject;
        }

        private static void HandleWhen(FoundryObject foundryObject, List<string> importLines, ref int currentIndex)
        {
            //there are two types of Whens and those can be two types too
            //they are When and HideWhen
            //they are with parameters (MAP_START, MANUAL) or with parameters (everything else)

            List<Trigger> whenObjects = new List<Trigger>();
            var split = importLines[currentIndex].Split(new char[] { ' ' });

            var whenType = split[0].Trim();
            var triggerType = Constants.Trigger.ObjectiveComplete.Title; //default is objective complete
            if (split.Length > 1)
            {
                triggerType = split[1];
            }

            //these triggers have no stats 
            if (triggerType.Equals(Constants.Trigger.MapStart) || 
                triggerType.Equals(Constants.Trigger.Manual) ||
                triggerType.Equals(Constants.Trigger.Component.CurrentComponentComplete) ||
                triggerType.Equals(Constants.Trigger.ObjectiveStart) ||
                triggerType.Equals(Constants.Trigger.MissionStart))
            {
                whenObjects.Add(new Trigger(triggerType));
            }
            else
            {
                var obj = GetFoundryObjectByIndex(importLines, ref currentIndex, triggerType);
                //check the COMPONENT_ID and OBJECTIVE_ID fields.  need to see if there are multiple components or objectives (not sure if objective is possible)
                if (obj.Fields.ContainsKey(Constants.Trigger.Component.ComponentID))
                {
                    var fieldValue = obj.Fields[Constants.Trigger.Component.ComponentID].ToString();
                    if (fieldValue.Contains(","))
                    {
                        var ids = fieldValue.Split(new char[] { ',' });
                        foreach (var id in ids)
                        {
                            var trig = new ComponentTrigger(triggerType);
                            trig.Fields[Constants.Trigger.Component.ComponentID] = id;
                            whenObjects.Add(trig);
                        }
                    }
                    else
                    {
                        whenObjects.Add((Trigger)obj);
                    }
                }
                else if (obj.Fields.ContainsKey(Constants.Trigger.ObjectiveComplete.ObjectiveID))
                {
                    var fieldValue = obj.Fields[Constants.Trigger.ObjectiveComplete.ObjectiveID].ToString();
                    if (fieldValue.Contains(","))
                    {
                        var ids = fieldValue.Split(new char[] { ',' });
                        foreach (var id in ids)
                        {
                            var trig = new ComponentTrigger(triggerType);
                            trig.Fields[Constants.Trigger.ObjectiveComplete.ObjectiveID] = id;
                            whenObjects.Add(trig);
                        }
                    }
                    else
                    {
                        whenObjects.Add((Trigger)obj);
                    }
                }
                else
                {
                    //if it doesn't have any of them just add the trigger
                    whenObjects.Add((Trigger)obj);
                }
            }
            if (foundryObject.Title.Equals(Constants.Component.Title))
            {
                var component = (Component)foundryObject;
                //got the object now set it in the proper spot
                if (whenType.Equals(Constants.Trigger.When))
                {
                    component.When.AddRange(whenObjects);
                }
                else //if (whenType.Equals(Constants.Trigger.HideWhen))
                {
                    component.HideWhen.AddRange(whenObjects);
                }
            }
            else //if (foundryObject.Title.Equals(Constants.Dialog.Action.Title))
            {
                var action = (DialogAction)foundryObject;
                if (whenType.Equals(Constants.Component.ShowWhen.Title))
                {
                    action.ShowWhen.AddRange(whenObjects);
                }
                else //if (whenType.Equals(Constants.Trigger.HideWhen))
                {
                    action.HideWhen.AddRange(whenObjects);
                }
            }
        }

        private static void HandleExternVar(FoundryObject foundryObject, List<string> importLines, ref int currentIndex)
        {
            var externVar = new ExternalVariable();
            var text = importLines[currentIndex];
            var endText = text.Replace(Constants.Component.ExternalVar.Title, Constants.Component.ExternalVar.End);
            //go until it finds the end
            for(; currentIndex < importLines.Count - 1; currentIndex++)
            {
                var currentText = importLines[currentIndex];
                if (currentText.Equals(endText))
                {
                    currentIndex++;
                    return;
                }

                //it's either going to be ExternVar Type or Specific Value.  if it isn't then oops I didn't handle it and it will be ignored
                if (currentText.Contains(Constants.Component.ExternalVar.Title))
                {
                    var name = currentText.Trim().Replace(Constants.Component.ExternalVar.Title, "");
                    externVar.Name = name;
                }
                else if (currentText.Contains(Constants.Component.ExternalVar.Type))
                {
                    var type = currentText.Trim().Replace(Constants.Component.ExternalVar.Type, "");
                    externVar.Type = type;
                }
                else if (currentText.Contains(Constants.Component.ExternalVar.SpecificValue.Title))
                {
                    HandleSpecificValue(externVar, importLines, ref currentIndex);
                }
            }

            foundryObject.ExternalVariables.Add(externVar.Name, externVar);
        }

        private static void HandleSpecificValue(ExternalVariable externVar, List<string> importLines, ref int currentIndex)
        {
            var specificValue = new SpecificValue();
            var text = importLines[currentIndex];
            var endText = text.Replace(Constants.Component.ExternalVar.SpecificValue.Title, Constants.Component.ExternalVar.SpecificValue.End);
            //go until it finds the end
            for (; currentIndex < importLines.Count - 1; currentIndex++)
            {
                var currentText = importLines[currentIndex];
                if (currentText.Equals(endText))
                {
                    currentIndex++;
                    return;
                }

                //it's either going to be  Type SpecificValue (ignored) StringVal or FloatVal
                if (currentText.Contains(Constants.Component.ExternalVar.SpecificValue.Type))
                {
                    var type = currentText.Trim().Replace(Constants.Component.ExternalVar.SpecificValue.Title, "");
                    specificValue.Type = type;
                }
                else if (currentText.Contains(Constants.Component.ExternalVar.SpecificValue.FloatVal))
                {
                    var floatVal = currentText.Trim().Replace(Constants.Component.ExternalVar.SpecificValue.FloatVal, "");
                    specificValue.FloatVal = floatVal;
                }
                else if (currentText.Contains(Constants.Component.ExternalVar.SpecificValue.StringVal))
                {
                    var stringVal = currentText.Trim().Replace(Constants.Component.ExternalVar.SpecificValue.StringVal, "");
                    specificValue.StringVal = stringVal;
                }
            }

            externVar.SpecificValue = specificValue;
        }
    }
}
