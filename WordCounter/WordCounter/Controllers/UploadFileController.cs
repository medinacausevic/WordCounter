using Grpc.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WordCounter.Model;
using X.PagedList;

namespace WordCounter.Controllers
{
    public class UploadFileController : Controller
    {
        [Obsolete]
        private IHostingEnvironment Environment;
        private DBContext db=new DBContext();
        private static readonly int pageSize = 20;

        [Obsolete]
        public UploadFileController(IHostingEnvironment _environment)
        {
            Environment = _environment;
        }


        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DisplayData(string sortOrder, int? page)
        {
            int pageNumber = (page ?? 1);
            ViewBag.SortOrder = sortOrder;
            IQueryable<WordModel> wordList = db.WordModels;

            wordList = sortOrder switch
            {
                "FrequencyASC" => wordList.OrderBy(x => x.Frequency).ThenBy(x => x.Word),
                "WordASC" => wordList.OrderBy(x => x.Word),
                "WordDESC" => wordList.OrderByDescending(x => x.Word),
                _ => wordList.OrderByDescending(x => x.Frequency).ThenBy(x => x.Word),
            };
            

            return View("Index", wordList.ToPagedList(pageNumber, pageSize));
        }
        

        [HttpPost]
        [Obsolete]
        public IActionResult Upload(IFormFile postedFile)
        {
            List<WordModel> wordList = new List<WordModel>();
            try
            {
                /*Prepare a local directory to upload file*/
                string wwwPath = Environment.WebRootPath;
                string path = Path.Combine(Environment.WebRootPath, "Uploads");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                
                
                /*Save the file to the local directory*/
                string fileName = Path.GetFileName(postedFile.FileName);
                /*Check file extension*/
                if (fileName.Split(".").Last() != "txt")
                {
                    throw new System.ArgumentException("AnotherExtension");
                }
                using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                }
                

                /*Read from file and get words*/
                var fileData = System.IO.File.ReadAllText(Path.Combine(path, fileName), Encoding.UTF8);
                if (fileData == "")
                {
                    throw new System.ArgumentException("Empty");
                }
                fileData = Regex.Replace(fileData, @"(\s+'+)|('+\s+)|[(){}\[\]]+", " ");
                var splitWords = Regex.Split(fileData, @"[\s\t\n\r,:;!?.""]+");
                

                /*Count repetitions of words*/
                wordList = splitWords.Where(s => s.Length > 1)
                    .GroupBy(s => s)
                    .Select(s => new WordModel { Word = s.Key, Frequency = s.Count() })
                    .OrderByDescending(wordModel => wordModel.Frequency).ThenBy(x => x.Word).ToList();


                /*Update database with new words*/
                db.WordModels.RemoveRange(db.WordModels);
                db.SaveChanges();
                foreach (var item in wordList)
                {
                    db.WordModels.Add(item);
                    db.SaveChanges();
                }

                if(wordList.Count==0)
                {
                    ViewBag.Message = "There are no words in the file!";
                }
            }
            catch (NullReferenceException)
            {
                ViewBag.Message = "The file was not uploaded!";
            }
            catch (IOException)
            {
                ViewBag.Message = "Sorry, there was a problem loading the file!";
            }
            catch (Exception e){
                ViewBag.Message = e.Message switch
                {
                    "Empty" => "The selected file is empty!",
                    "AnotherExtension" => "Please choose .txt file!",
                    _ => "Application ran into a problem. Please, choose file again.",
                };
            }


            return View("Index", wordList.ToPagedList(1, pageSize));
        }
    }
}

