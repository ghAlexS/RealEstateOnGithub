namespace RealEstate.Rentals
{
	using System.Linq;
	using System.Threading.Tasks;
	using System.Web;
	using System.Web.Mvc;
	using App_Start;
	using MongoDB.Bson;
	using MongoDB.Bson.IO;
	using MongoDB.Driver;
	using MongoDB.Driver.GridFS;
	using MongoDB.Driver.Linq;

	public class RentalsController : Controller
	{
		public readonly RealEstateContextNewApis ContextNew = new RealEstateContextNewApis();

		public async Task<ActionResult> Index(RentalsFilter filters)
		{
			var rentals = await FilterRentals(filters)
				.Select(r => new RentalViewModel
				{
					Id = r.Id,
					Address = r.Address,
					Description = r.Description,
					NumberOfRooms = r.NumberOfRooms,
					Price = r.Price
				})
				.OrderBy(r => r.Price)
				.ThenByDescending(r => r.NumberOfRooms)
				.ToListAsync();
			var model = new RentalsList
			{
				Rentals = rentals,
				Filters = filters
			};
			return View(model);
		}

		private IMongoQueryable<Rental> FilterRentals(RentalsFilter filters)
		{
			var rentals = ContextNew.Rentals.AsQueryable();

			if (filters.MinimumRooms.HasValue)
			{
				rentals = rentals
					.Where(r => r.NumberOfRooms >= filters.MinimumRooms);
			}

			if (filters.PriceLimit.HasValue)
			{
				rentals = rentals
					.Where(r => r.Price <= filters.PriceLimit);
			}

			return rentals;
		}

		public ActionResult Post()
		{
			return View();
		}

		[HttpPost]
		public async Task<ActionResult> Post(PostRental postRental)
		{
			var rental = new Rental(postRental);
			await ContextNew.Rentals.InsertOneAsync(rental);
			return RedirectToAction("Index");
		}

		public ActionResult AdjustPrice(string id)
		{
			var rental = GetRental(id);
			return View(rental);
		}

		private Rental GetRental(string id)
		{
			//var rental = Context.Rentals.FindOneById(new ObjectId(id));
			var rental = ContextNew.Rentals
				.Find(r => r.Id == id)
				.FirstOrDefault();
			return rental;
		}

		[HttpPost]
		public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
		{
			var rental = GetRental(id);
			rental.AdjustPrice(adjustPrice);
			//Context.Rentals.Save(rental);
			await ContextNew.Rentals.ReplaceOneAsync(r => r.Id == id, rental);
			return RedirectToAction("Index");
		}

		//[HttpPost]
		//public async Task<ActionResult> AdjustPrice(string id, AdjustPrice adjustPrice)
		//{
		//	var rental = GetRental(id);
		//	var adjustment = new PriceAdjustment(adjustPrice, rental.Price);
		//	var modificationUpdate = Builders<Rental>.Update
		//		.Push(r => r.Adjustments, adjustment)
		//		.Set(r => r.Price, adjustPrice.NewPrice);
		//	//Context.Rentals.Update(Query.EQ("_id", new ObjectId(id)), modificationUpdate);
		//	await ContextNew.Rentals.UpdateOneAsync(r => r.Id == id, modificationUpdate);
		//	return RedirectToAction("Index");
		//}

		public async Task<ActionResult> Delete(string id)
		{
			//Context.Rentals.Remove(Query.EQ("_id", new ObjectId(id)));
			await ContextNew.Rentals.DeleteOneAsync(r => r.Id == id);
			return RedirectToAction("Index");
		}

		public string PriceDistribution()
		{
			return new QueryPriceDistribution()
				.RunLinq(ContextNew.Rentals)
				.ToJson();
		}

		public ActionResult AttachImage(string id)
		{
			var rental = GetRental(id);
			return View(rental);
		}

		[HttpPost]
		public async Task<ActionResult> AttachImage(string id, HttpPostedFileBase file)
		{
			var rental = GetRental(id);
			if (rental.HasImage())
			{
				await DeleteImageAsync(rental);
			}
			await StoreImageAsync(file, id);
			return RedirectToAction("Index");
		}

		private async Task DeleteImageAsync(Rental rental)
		{
			await ContextNew.ImagesBucket.DeleteAsync(new ObjectId(rental.ImageId));
			await SetRentalImageIdAsync(rental.Id, null);
		}

		private async Task StoreImageAsync(HttpPostedFileBase file, string rentalId)
		{
			var options = new GridFSUploadOptions
			{
				Metadata = new BsonDocument("contentType", file.ContentType)
			};
			var imageId = await ContextNew.ImagesBucket
				.UploadFromStreamAsync(file.FileName, file.InputStream, options);
			await SetRentalImageIdAsync(rentalId, imageId.ToString());
		}

		private async Task SetRentalImageIdAsync(string rentalId, string imageId)
		{
			var setRentalImageId = Builders<Rental>.Update.Set(r => r.ImageId, imageId);
			await ContextNew.Rentals.UpdateOneAsync(r => r.Id == rentalId, setRentalImageId);
		}

		public ActionResult GetImage(string id)
		{
			try
			{
				var stream = ContextNew.ImagesBucket.OpenDownloadStream(new ObjectId(id));
				var contentType = stream.FileInfo.ContentType 
					?? stream.FileInfo.Metadata["contentType"].AsString;
				return File(stream, contentType);
			}
			catch (GridFSFileNotFoundException)
			{
				return HttpNotFound();
			}
		}

		public ActionResult JoinPreLookup()
		{
			var rentals = ContextNew.Rentals.Find(new BsonDocument()).ToList();
			var rentalZips = rentals.Select(r => r.ZipCode).Distinct().ToArray();

			var zipsById = ContextNew.Database.GetCollection<ZipCode>("zips")
				.Find(z => rentalZips.Contains(z.Id))
				.ToList()
				.ToDictionary(d => d.Id);

			var report = rentals
				.Select(r => new
				{
					Rental = r,
					ZipCode = r.ZipCode != null && zipsById.ContainsKey(r.ZipCode)
						? zipsById[r.ZipCode] : null
				});

			return Content(report.ToJson(new JsonWriterSettings {OutputMode = JsonOutputMode.Strict}), "application/json");
		}

		public ActionResult JoinWithLookup()
		{
			var report = ContextNew.Rentals
				.Aggregate()
				.Lookup<Rental, ZipCode, RentalWithZipCodes>(
					ContextNew.Database.GetCollection<ZipCode>("zips"),
					r => r.ZipCode,
					z => z.Id,
					w => w.ZipCodes
				)
				.ToList();

			return Content(report.ToJson(new JsonWriterSettings {OutputMode = JsonOutputMode.Strict}), "application/json");
		}
	}

	public class RentalWithZipCodes : Rental
	{
		public ZipCode[] ZipCodes { get; set; }
	}
}