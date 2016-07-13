﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FoundryMissionsCom.Models;
using FoundryMissionsCom.Models.FoundryMissionModels;
using FoundryMissionsCom.Models.FoundryMissionModels.Enums;
using FoundryMissionsCom.Models.FoundryMissionViewModels;
using FoundryMissionsCom.Helpers;
using FoundryMissionsCom.Attributes;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Linq.Expressions;

namespace FoundryMissionsCom.Controllers
{
    public class MissionsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Missions
        public ActionResult Index()
        {
            return RedirectToAction("index", "home");
            //return View(db.Missions.ToList());
        }

        // GET: Missions/5
        public ActionResult Details(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return RedirectToAction("index", "home");
            }
            Mission mission = db.Missions.Where(m => m.MissionLink.Equals(link)).FirstOrDefault();
            if (mission == null)
            {
                return HttpNotFound();
            }

            //if it is in review or unpubilshed only an aadministrator or the author can view it
            if (mission.Status == Models.FoundryMissionModels.Enums.MissionStatus.InReview ||
                mission.Status == Models.FoundryMissionModels.Enums.MissionStatus.Unpublished)
            {
                if (!mission.Author.UserName.Equals(User.Identity.Name) &&
                   (!User.IsInRole("Administrator")))
                {
                    return HttpNotFound();
                }
            }

            //if it is removed only an admnistrator can view it
            if (mission.Status == Models.FoundryMissionModels.Enums.MissionStatus.Removed)
            {
                if (!User.IsInRole("Administrator"))
                {
                    return HttpNotFound();
                }
            }

            ViewMissionViewModel viewMission = new ViewMissionViewModel()
            {
                Id = mission.Id,
                Author = mission.Author,
                CrypticId = mission.CrypticId.ToUpper(),
                Name = mission.Name,
                Description = mission.Description,
                Faction = mission.Faction,
                FactionImageUrl = MissionHelper.GetBigFactionImageUrl(mission.Faction),
                MinimumLevel = mission.MinimumLevel,
                MinimumLevelImageUrl = MissionHelper.GetBigLevelImageUrl(mission.MinimumLevel, mission.Faction),
                DateLastUpdated = mission.DateLastUpdated,
                Length = mission.Length,
                Tags = mission.Tags.OrderBy(t => t.TagName).ToList(),
                Videos = mission.Videos,
                Status = mission.Status,
                MissionLink = mission.MissionLink,
                Images = new List<string>(),
            };

            //It's okay to show the mission now

            return View(viewMission);
        }

        // GET: Missions/Submit
        [Authorize]
        public ActionResult Submit()
        {
            List<SelectListItem> publishedSelectItems = MissionHelper.GetYesNoSelectList();
            ViewBag.AvailableTags = db.MissionTagTypes.Select(t => t.TagName).ToList();
            ViewBag.PublishedSelectList = new SelectList(publishedSelectItems, "Value", "Text");
            ViewBag.MinimumLevelSelectList = new SelectList(MissionHelper.GetMinimumLevelSelectList(), "Value", "Text");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult Submit([Bind(Include = "CrypticId,Name,Description,Length,Faction,MinimumLevel,Spotlit,Published,Tags")] SubmitMissionViewModel missionViewModel, string submitButton)
        {
            if (ModelState.IsValid)
            {
                //check if cryptic id is already used
                if (db.Missions.Any(m => m.CrypticId.Equals(missionViewModel.CrypticId)))
                {
                    ModelState.AddModelError("DuplicateCrypticID", "Duplicate Cryptic ID.");

                    List<SelectListItem> publishedSelectItems = MissionHelper.GetYesNoSelectList();
                    ViewBag.AvailableTags = db.MissionTagTypes.Select(t => t.TagName).ToList();
                    ViewBag.PublishedSelectList = new SelectList(publishedSelectItems, "Value", "Text");
                    return View(missionViewModel);
                }

                ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName.Equals(User.Identity.Name));
                Mission mission = new Mission();

                #region Copy Info

                mission.CrypticId = missionViewModel.CrypticId.ToUpper();
                mission.Description = missionViewModel.Description;
                mission.Faction = missionViewModel.Faction;
                mission.Length = missionViewModel.Length;
                mission.MinimumLevel = missionViewModel.MinimumLevel;
                mission.Name = missionViewModel.Name;
                mission.Published = missionViewModel.Published;
                mission.Spotlit = missionViewModel.Spotlit;

                #endregion

                mission.Tags = db.MissionTagTypes.Where(t => missionViewModel.Tags.Contains(t.TagName)).ToList();
                
                mission.MissionLink = MissionHelper.GetMissionLink(db, mission);
                mission.Author = user;
                mission.DateAdded = DateTime.Today;
                mission.DateLastUpdated = DateTime.Today;
                if (submitButton.Equals("Save and Publish"))
                {
                    if (user.AutoApproval)
                    {
                        mission.Status = Models.FoundryMissionModels.Enums.MissionStatus.Published;
                    }
                    else
                    {
                        mission.Status = Models.FoundryMissionModels.Enums.MissionStatus.InReview;
                        //mission.Status = Models.FoundryMissionModels.Enums.MissionStatus.Published;
                    }
                }
                else //if (submitButton.Equals("Save"))
                {
                    //don't do anything, leave it at the current status (default = unpublished)
                    //mission.Status = Models.FoundryMissionModels.Enums.MissionStatus.Unpublished;
                }
                mission.Spotlit = false;                

                db.Missions.Add(mission);
                db.SaveChanges();
                return RedirectToAction("details", new { link = mission.MissionLink });
            }

            return View(missionViewModel);
        }

        public ActionResult Edit(string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return RedirectToAction("index", "home");
            }
            Mission mission = db.Missions.Where(m => m.MissionLink.Equals(link)).FirstOrDefault();
            if (mission == null)
            {
                return HttpNotFound();
            }

            //only people who can edit a mission are the author or an admin
            if (!mission.Author.UserName.Equals(User.Identity.Name) && !User.IsInRole(ConstantsHelper.AdminRole))
            {

                return HttpNotFound();
            }

            //if the user is not an admin and it is removed it doesn't exist
            if (mission.Status == MissionStatus.Removed && !User.IsInRole(ConstantsHelper.AdminRole))
            {
                return HttpNotFound();
            }

            var publishedSelectItems = new List<SelectListItem>();
            #region Published Select List
            publishedSelectItems.Add(new SelectListItem()
            {
                Value = "false",
                Text = "No",
            });
            publishedSelectItems.Add(new SelectListItem()
            {
                Value = "true",
                Text = "Yes",
            });
            #endregion

            var editModel = new EditMissionViewModel();
            editModel.Id = mission.Id;
            editModel.CrypticId = mission.CrypticId;
            editModel.Name = mission.Name;
            editModel.Description = mission.Description;
            editModel.Length = mission.Length;
            editModel.Faction = mission.Faction;
            editModel.MinimumLevel = mission.MinimumLevel;
            editModel.Spotlit = mission.Spotlit;
            editModel.Published = mission.Published;
            editModel.Status = mission.Status;
            editModel.Author = mission.Author;
            editModel.AutoApprove = mission.Author.AutoApproval;
            editModel.Tags = mission.Tags.Select(t => t.TagName).ToList();
            mission.MissionLink = MissionHelper.GetMissionLink(db, mission);

            var unselectedTags = db.MissionTagTypes.Select(t => t.TagName).ToList();
            foreach (var tags in editModel.Tags)
            {
                unselectedTags.Remove(tags);
            }
            ViewBag.AvailableTags = unselectedTags;
            ViewBag.PublishedSelectList = new SelectList(publishedSelectItems, "Value", "Text");
            ViewBag.MinimumLevelSelectList = new SelectList(MissionHelper.GetMinimumLevelSelectList(), "Value", "Text");

            return View(editModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [MultipleButton(Name = "action", Argument = "publishmission")]
        public ActionResult PublishMission([Bind(Include = "Id,Author,CrypticId,Name,Description,Length,Faction,MinimumLevel,Spotlit,Published, Tags")] EditMissionViewModel missionViewModel)
        {
            var mission = db.Missions.Find(missionViewModel.Id);
            var author = mission.Author;
            var UserManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(db));
            if (author.AutoApproval || UserManager.IsInRole(author.Id, ConstantsHelper.AdminRole))
            {
                missionViewModel.Status = MissionStatus.Published;
            }
            else
            {
                missionViewModel.Status = MissionStatus.InReview;
            }

            return Edit(missionViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [MultipleButton(Name = "action", Argument = "savemission")]
        public ActionResult SaveMission([Bind(Include = "Id,Author,CrypticId,Name,Description,Length,Faction,MinimumLevel,Spotlit,Published, Tags")] EditMissionViewModel missionViewModel)
        {
            return Edit(missionViewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [MultipleButton(Name = "action", Argument = "withdrawmission")]
        public ActionResult WithdrawMission([Bind(Include = "Id,Author,CrypticId,Name,Description,Length,Faction,MinimumLevel,Spotlit,Published, Tags")] EditMissionViewModel missionViewModel)
        {
            missionViewModel.Status = MissionStatus.Unpublished;
            return Edit(missionViewModel);
        }

        private ActionResult Edit(EditMissionViewModel missionViewModel)
        {
            if (ModelState.IsValid)
            {
                var mission = db.Missions.Find(missionViewModel.Id);
                var user = mission.Author;              
                mission.CrypticId = missionViewModel.CrypticId.ToUpper();
                mission.Name = missionViewModel.Name;
                mission.Description = missionViewModel.Description;
                mission.Length = missionViewModel.Length;
                mission.Faction = missionViewModel.Faction;
                mission.MinimumLevel = missionViewModel.MinimumLevel;
                mission.Spotlit = missionViewModel.Spotlit;
                mission.Status = missionViewModel.Status;
                mission.DateLastUpdated = DateTime.Today;


                // in case the tags list is null create a blank one
                if (missionViewModel.Tags == null)
                {
                    missionViewModel.Tags = new List<string>();
                }
                //first remove the tags that don't exist anymore
                var currentTags = mission.Tags.ToList();
                foreach(var tag in currentTags)
                {
                    if (!missionViewModel.Tags.Contains(tag.TagName))
                    {
                        mission.Tags.Remove(tag);
                    }
                }
                //now add the tags that don't exist
                foreach (var tag in missionViewModel.Tags)
                {
                    var tagType = db.MissionTagTypes.FirstOrDefault(t => t.TagName.Equals(tag));
                    if (tagType != null)
                    {
                        if (!mission.Tags.Contains(tagType))
                        {
                            mission.Tags.Add(tagType);
                        }
                    }
                }

                db.SaveChanges();

                return RedirectToAction("details", new { link = mission.MissionLink });
            }
            return View(missionViewModel);
        }

        public ActionResult Random()
        {
            var missionLink = db.Missions.OrderBy(m => Guid.NewGuid()).Where(m=> m.Status == MissionStatus.Published).Select(m => m.MissionLink).FirstOrDefault();

            return RedirectToAction("details", new { link = missionLink });
        }

        public ActionResult Search(string q, int? page)
        {

            if (string.IsNullOrEmpty(q))
            {
                return RedirectToAction("Index", "Home");
            }

            int pageNumber = 1;
            if (page != null)
            {
                pageNumber = (int)page;
            }

            
            string upperQuery = q.Trim().ToUpper();

            #region Get Missions
            var missions = db.Missions.Where(m => (m.Author.UserName.ToUpper().Contains(upperQuery) ||
                                             m.CrypticId.ToUpper().Contains(upperQuery) ||
                                             m.Description.ToUpper().Contains(upperQuery) ||
                                             m.Name.ToUpper().Contains(upperQuery)) &&
                                             m.Status == MissionStatus.Published)
                                       .OrderByDescending(m => m.DateLastUpdated)
                                       .Skip(ConstantsHelper.MissionsPerPage * (pageNumber-1))
                                       .Take(ConstantsHelper.MissionsPerPage)
                                       .ToList();
            List<ListMissionViewModel> listMissions = MissionHelper.GetListMissionViewModels(missions);
            #endregion

            #region Paging Info

            var missionCount = db.Missions.Where(m => (m.Author.UserName.ToUpper().Contains(upperQuery) ||
                                             m.CrypticId.ToUpper().Contains(upperQuery) ||
                                             m.Description.ToUpper().Contains(upperQuery) ||
                                             m.Name.ToUpper().Contains(upperQuery)) &&
                                             m.Status == MissionStatus.Published)
                                       .Count();

            var totalPages = (missionCount + ConstantsHelper.MissionsPerPage -1 ) / ConstantsHelper.MissionsPerPage;

            var pagesCounter = pageNumber - (ConstantsHelper.PagesToShow / 2);
            if (pagesCounter < 1)
            {
                pagesCounter = 1;
            }
            else if (pagesCounter + ConstantsHelper.PagesToShow > totalPages)
            {
                pagesCounter = totalPages - ConstantsHelper.PagesToShow + 1;
            }

            ViewBag.Query = q;
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.MissionCount = missionCount;
            ViewBag.StartPage = pagesCounter;

            #endregion

            return View(listMissions);
        }

        [ActionName("Advanced-Search")]
        public ActionResult AdvancedSearch()
        {
            ViewBag.AvailableTags = db.MissionTagTypes.Select(t => t.TagName).ToList();
            ViewBag.MinimumLevelSelectList = new SelectList(MissionHelper.GetMinimumLevelSelectList(true), "Value", "Text");
            return View();
        }
        
        public ActionResult Author(string author)
        {
            return RedirectToAction("index", "home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SearchResults(AdvancedSearchViewModel model, int? page)
        {
            if (model == null)
            {
                RedirectToAction("advanced-search");
            }

            //if everything in the model is null also go back
            if (string.IsNullOrWhiteSpace(model.Author) &&
                (model.Faction == null) &&
                (model.MinimumLevel == null) &&
                (model.Tags == null || model.Tags.Count == 0) &&
                string.IsNullOrWhiteSpace(model.Title))
            {
                RedirectToAction("advanced-search");
            }

            int pageNumber = 1;
            if (page != null)
            {
                pageNumber = (int)page;
            }

            //build the query
            var qry = db.Missions.AsQueryable();
            if (!string.IsNullOrWhiteSpace(model.Title))
            {
                qry = qry.Where(m => m.Name.ToUpper().Contains(model.Title.ToUpper()));
            }
            if (model.Faction != null)
            {
                qry = qry.Where(m => m.Faction == model.Faction);
            }

            if (!string.IsNullOrWhiteSpace(model.Author))
            {
                qry = qry.Where(m => m.Author.CrypticTag.ToUpper().Contains(model.Author.ToUpper()));
            }

            if (model.MinimumLevel != null)
            {
                qry = qry.Where(m => m.MinimumLevel <= model.MinimumLevel);
            }

            List<Mission> missions = qry.OrderByDescending(m => m.DateLastUpdated).ToList();
            List<Mission> filteredMissions = new List<Mission>();

            //filter on the tags
            if (model.Tags != null && model.Tags.Count > 0)
            {
                List<MissionTagType> tags = db.MissionTagTypes.Where(t => model.Tags.Contains(t.TagName)).ToList();               

                foreach(var mission in missions)
                {
                    if (mission.Tags.Intersect(tags).Count() == tags.Count)
                    {
                        filteredMissions.Add(mission);
                    }
                }

            }
            else
            {
                filteredMissions = missions;
            }

            var missionCount = filteredMissions.Count();

            missions = filteredMissions.Skip(ConstantsHelper.MissionsPerPage * (pageNumber - 1)).Take(ConstantsHelper.MissionsPerPage).ToList();
            List<ListMissionViewModel> listmissions = MissionHelper.GetListMissionViewModels(missions);
            model.Missions = listmissions;



            #region Paging Info

            var totalPages = (missionCount + ConstantsHelper.MissionsPerPage - 1) / ConstantsHelper.MissionsPerPage;

            var pagesCounter = pageNumber - (ConstantsHelper.PagesToShow / 2);
            if (pagesCounter < 1)
            {
                pagesCounter = 1;
            }
            else if (pagesCounter + ConstantsHelper.PagesToShow > totalPages)
            {
                pagesCounter = totalPages - ConstantsHelper.PagesToShow + 1;
            }

            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.MissionCount = missionCount;
            ViewBag.StartPage = pagesCounter;

            #endregion

            return View(model);
        }




        #region  Auto generated

        // GET: Missions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Mission mission = db.Missions.Find(id);
            if (mission == null)
            {
                return HttpNotFound();
            }
            return View(mission);
        }

        // POST: Missions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Mission mission = db.Missions.Find(id);
            db.Missions.Remove(mission);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
