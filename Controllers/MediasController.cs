using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DAL;
using Models;
using static Controllers.AccessControl;
namespace Controllers
{

    public class MediasController : Controller
    {

        private void InitSessionVariables()
        {
            // Session is a dictionary that hold keys values specific to a session
            // Each user of this web application have their own Session
            // A Session has a default time out of 20 minutes, after time out it is cleared

            if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
            if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
            if (Session["Search"] == null) Session["Search"] = false;
            if (Session["SearchString"] == null) Session["SearchString"] = "";
            if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
            if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
            if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
            if (Session["SortAscending"] == null) Session["SortAscending"] = true;
            ValidateSelectedCategory();
        }

        private void ResetCurrentMediaInfo()
        {
            Session["CurrentMediaId"] = 0;
            Session["CurrentMediaTitle"] = "";
        }

        private void ValidateSelectedCategory()
        {
            if (Session["SelectedCategory"] != null)
            {
                var selectedCategory = (string)Session["SelectedCategory"];
                var Medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
                if (Medias.Count() == 0)
                    Session["SelectedCategory"] = "";
            }
        }
        [UserAccess(Models.Access.View)]
        public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
        {
            try
            {
                InitSessionVariables();

                bool search = (bool)Session["Search"];

                if (search)
                {
                    return PartialView();
                }
                return null;
            }
            catch (System.Exception ex)
            {
                return Content("Erreur interne" + ex.Message, "text/html");
            }
        }
        // This action produce a partial view of Medias
        // It is meant to be called by an AJAX request (from client script)
        [UserAccess(Models.Access.View)]
        public ActionResult GetMedias(bool forceRefresh = false)
        {
            try
            {
                IEnumerable<Media> result = null;
                // Must evaluate HasChanged before forceRefresh, this will fix an usefull refresh
                if (DB.Medias.HasChanged || forceRefresh)
                {
                    // forceRefresh is true when a related view is produce
                    // DB.Medias.HasChanged is true when a change has been applied on any Media

                    InitSessionVariables();
                    bool search = (bool)Session["Search"];
                    string searchString = (string)Session["SearchString"];
                    string selectedCategory = (string)Session["SelectedCategory"];

                    User connectedUser = Models.User.ConnectedUser;
                    if (connectedUser.IsAdmin)
                    {
                        result = DB.Medias.ToList();
                    }
                    else
                    {
                        result = DB.Medias.ToList()
                            .Where(m => m.OwnerId == connectedUser.Id || m.Shared);
                    }

                    if (search)
                    {
                        result = result.Where(m => m.Title.ToLower().Contains(searchString));
                    }

     
                    if (selectedCategory != "")
                    {
                        result = result.Where(m => m.Category == selectedCategory);
                    }

                    if ((bool)Session["SortAscending"])
                    {
                        if ((bool)Session["SortByTitle"])
                            result = result.OrderBy(m => m.Title);
                        else
                            result = result.OrderBy(m => m.PublishDate);
                    }
                    else
                    {
                        if ((bool)Session["SortByTitle"])
                            result = result.OrderByDescending(m => m.Title);
                        else
                            result = result.OrderByDescending(m => m.PublishDate);
                    }
                    return PartialView(result);
                }
                return null;
            }
            catch (System.Exception ex)
            {
                return Content("Erreur interne" + ex.Message, "text/html");
            }
        }

        [UserAccess(Models.Access.View)]
        public ActionResult List()
        {
            ResetCurrentMediaInfo();
            return View();
        }
        [UserAccess(Models.Access.View)]
        public ActionResult ToggleSearch()
        {
            if (Session["Search"] == null) Session["Search"] = false;
            Session["Search"] = !(bool)Session["Search"];
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult SortByTitle()
        {
            Session["SortByTitle"] = true;
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult ToggleSort()
        {
            Session["SortAscending"] = !(bool)Session["SortAscending"];
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult SortByDate()
        {
            Session["SortByTitle"] = false;
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult SetSearchString(string value)
        {
            Session["SearchString"] = value.ToLower();
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult SetSearchCategory(string value)
        {
            Session["SelectedCategory"] = value;
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult About()
        {
            return View();
        }

        [UserAccess(Models.Access.View)]
        public ActionResult Details(int id)
        {
            Session["CurrentMediaId"] = id;
            Media media = DB.Medias.Get(id);
            if (media != null)
            {
                User connectedUser = Models.User.ConnectedUser;

                if (!connectedUser.IsAdmin && media.OwnerId != connectedUser.Id && !media.Shared)
                    return RedirectToAction("List");
                Session["CurrentMediaTitle"] = media.Title;
                return View(media);
            }
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.View)]
        public ActionResult GetMediaDetails(bool forceRefresh = false)
        {
            try
            {
                int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;

                if (DB.Medias.HasChanged || forceRefresh)
                {
                    Media media = DB.Medias.Get(id);
                    if (media != null)
                        return PartialView("GetMediaDetails", media);
                }

                return null;
            }
            catch (System.Exception ex)
            {
                return Content("Erreur interne " + ex.Message, "text/html");
            }
        }
        [UserAccess(Models.Access.Write)]
        public ActionResult Create()
        {
            return View(new Media());
        }

        [HttpPost]
        /* Install anti forgery token verification attribute.
         * the goal is to prevent submission of data from a page 
         * that has not been produced by this application*/
        [ValidateAntiForgeryToken()]
        [UserAccess(Models.Access.Write)]
        public ActionResult Create(Media Media, string SharedCB="off")
        {
            Media.OwnerId = Models.User.ConnectedUser.Id;
            Media.Shared = SharedCB == "on";
            DB.Medias.Add(Media);
            return RedirectToAction("List");
        }
        [UserAccess(Models.Access.Write)]
        public ActionResult Edit()
        {
            // Note that id is not provided has a parameter.
            // It use the Session["CurrentMediaId"] set within
            // Details(int id) action
            // This way we prevent from malicious requests that could
            // modify or delete programatically the all the Medias

            int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
            if (id != 0)
            {
                Media Media = DB.Medias.Get(id);
                if (Media != null)
                    return View(Media);
            }
            return RedirectToAction("List");
        }

        [HttpPost]
        [ValidateAntiForgeryToken()]
        [UserAccess(Models.Access.Write)]
        public ActionResult Edit(Media Media, string SharedCB = "off")
        {
            // Has explained earlier, id of Media is stored server side an not provided in form data
            // passed in the method in order to prever from malicious requests

            int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;

            // Make sure that the Media of id really exist
            Media storedMedia = DB.Medias.Get(id);
            if (storedMedia != null)
            {
                Media.Id = id; // patch the Id
                Media.PublishDate = storedMedia.PublishDate; // keep orignal PublishDate
                Media.OwnerId = storedMedia.OwnerId;   
                Media.Shared = SharedCB == "on";
                DB.Medias.Update(Media);
            }
            return RedirectToAction("Details/" + id);
        }
        [UserAccess(Models.Access.Write)]
        public ActionResult Delete()
        {
            int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
            if (id != 0)
            {
                DB.Medias.Delete(id);
            }
            return RedirectToAction("List");
        }

        // This action is meant to be called by an AJAX request
        // Return true if there is a name conflict
        // Look into validation.js for more details
        // and also into Views/Medias/MediaForm.cshtml
        public JsonResult CheckConflict(string YoutubeId)
        {
            int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
            // Response json value true if name is used in other Medias than the current Media
            return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(),
                        JsonRequestBehavior.AllowGet /* must have for CORS verification by client browser */);
        }

        


    }
}
