using FileUploads.Models;
using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FileUploads.Controllers
{
    public class HomeController : Controller
    {
        LargeUploadEntities db = new LargeUploadEntities();

        public ActionResult Index()
        {
            var data = db.fileuploads.ToList();
            return View(data);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult MultiUpload()
        {
            try
            {
                var chunk = Request.Files["chunk"];
                var chunkIndex = int.Parse(Request.Form["chunkIndex"]);
                var totalChunks = int.Parse(Request.Form["totalChunks"]);
                var fileName = Request.Form["fileName"];
                var fileId = Request.Form["fileId"];

                if (chunk != null && chunk.ContentLength > 0)
                {
                    string tempPath = Server.MapPath("~/App_Data/Uploads/Temp/" + fileId);
                    if (!Directory.Exists(tempPath))
                        Directory.CreateDirectory(tempPath);

                    string chunkPath = Path.Combine(tempPath, chunkIndex.ToString());
                    chunk.SaveAs(chunkPath);

                    return Json(new { success = true });
                }
                return Json(new { success = false, message = "No file received." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult UploadComplete(string fileName, string fileId, bool completed)
        {
            if (completed)
            {
                try
                {
                    string tempPath = Server.MapPath("~/App_Data/Uploads/Temp/" + fileId);
                    string finalPath = Server.MapPath("~/App_Data/Uploads");
                    string newPath = Path.Combine(finalPath, fileName);

                    // Ensure the directories exist
                    Directory.CreateDirectory(finalPath);

                    using (var fs = new FileStream(newPath, FileMode.Create))
                    {
                        string[] chunkFiles = Directory.GetFiles(tempPath).OrderBy(f => int.Parse(Path.GetFileName(f))).ToArray();
                        foreach (string chunkFile in chunkFiles)
                        {
                            byte[] chunkBytes = System.IO.File.ReadAllBytes(chunkFile);
                            fs.Write(chunkBytes, 0, chunkBytes.Length);
                        }
                    }

                    // Delete temp files after merging
                    Directory.Delete(tempPath, true);

                    // Save to database
                    var fileUpload = new fileupload
                    {
                        filename = fileName,
                        file_path = "~/App_Data/Uploads/" + fileName
                    };
                    db.fileuploads.Add(fileUpload);
                    db.SaveChanges();

                    return Json(new { success = true });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = ex.Message });
                }
            }
            return Json(new { success = false, message = "Upload not completed" });
        }

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