using System.Web.Mvc;
using RealEstate.App_Start;

namespace RealEstate.Controllers
{
    using System.Threading.Tasks;
    using MongoDB.Bson;

    public class HomeController : Controller
	{
		public static RealEstateContextNewApis Context = new RealEstateContextNewApis();

		public async Task<ActionResult> Index()
		{
			var buildInfoCommand = new BsonDocument("buildinfo", 1);
			var buildInfo = await Context.Database.RunCommandAsync<BsonDocument>(buildInfoCommand);
			return Content(buildInfo.ToJson(), "application/json");
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your application description page.";

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
	}
}