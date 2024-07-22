using GoogleApi.Entities.Common.Enums;
using GoogleApi.Entities.Places.Details.Request;
using GoogleApi.Entities.Places.Details.Request.Enums;
using GoogleApi.Entities.Places.Search.Text.Request;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Models;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using Travel_App_Web.Data;

namespace Services
{
    public class TourBuilder
    {
        private AmadeusService amadeusService;
        private NavigationManager navigationManager;
        private readonly DBContext _context;
        private string googleApiKey { get; set; }
        public Tour Tour { get; set; } = new();
        public ConcurrentDictionary<string, Models.Hotel> Hotels = new();
        public Dictionary<string, List<HotelsOffer>> HotelsOffers = new();
        public ConcurrentDictionary<string, Activity> Activities = new();

        public TourBuilder(AmadeusService _amadeusService, NavigationManager _navigationManager, DBContext context, IConfiguration _configuration)
        {
            amadeusService = _amadeusService;
            navigationManager = _navigationManager;
            _context = context;

            googleApiKey = _configuration["GOOGLE_MAPS_API"];
        }

        public void AddHotel(string hotelId)
        {
            Tour.Hotels.Add(Hotels[hotelId]);
            Tour.HotelsOffers.AddRange(HotelsOffers[hotelId]);
            Tour.HotelsOffers = Tour.HotelsOffers.OrderBy(ho => ho.CheckOutDate).ToList();
            Hotels = new();
            HotelsOffers = new();
        }
        public void AddActivity(Activity activity)
        {
            Tour.Program.Add(activity);
            Tour.Program = Tour.Program.OrderBy(p => p.StartDate).ToList();
            Activities = new();
        }

        public void RemoveActivity(Activity activity)
        {
            Tour.Program.Remove(activity);
        }
        public void RemoveHotelOffers(string offerId)
        {
            var offer = Tour.HotelsOffers.FirstOrDefault(ho => ho.OfferId == offerId);
            var hotel = Tour.Hotels.FirstOrDefault(h => h.HotelId == offer.HotelId);
            if (offer != null && hotel != null)
            {
                Tour.HotelsOffers.RemoveAll(ho => ho.CheckInDate.Date == offer.CheckInDate.Date && ho.CheckOutDate.Date == offer.CheckOutDate.Date);
                Tour.Program.RemoveAll(a =>
                {
                    if (a is TransferOffer to)
                    {
                        return (to.EndLocation.PlaceId == hotel.PlaceId ||
                        (Math.Abs(to.EndLocation.Latitude - hotel.Location.Latitude) <= 0.0001 && Math.Abs(to.EndLocation.Longitude - hotel.Location.Longitude) <= 0.0001))
                        && (to.EndDate.Date == offer.CheckInDate.Date || to.EndDate.AddDays(1).Date == offer.CheckInDate.Date);
                    }
                    else
                        return false;
                });
                Tour.Hotels.Remove(hotel);
            }
        }
        public void RemoveTransfers()
        {
            if (Tour.Program.Count > 0)
                Tour.Program.RemoveAll(ac => ac is TransferOffer);
        }

        public async Task BuildAsync()
        {
            if (Tour.Agency is not null && Tour.Agency.Country is not null)
            {
                try
                {
                    _context.Attach(Tour.Agency);
                }
                catch
                {
                    _context.Update(Tour.Agency);
                }
            }

            var countryDict = new Dictionary<string, Country>();

            var cityDict = new Dictionary<long, City>();

            var tourCities = new List<City>();

            foreach (var city in Tour.Cities)
            {
                if (!cityDict.ContainsKey(city.CityId))
                {
                    var existingCity = await _context.Cities
                                                     .Include(c => c.Country)
                                                     .FirstOrDefaultAsync(c => c.CityId == city.CityId);

                    if (existingCity != null)
                    {
                        cityDict[city.CityId] = existingCity;
                    }
                    else
                    {
                        if (!countryDict.ContainsKey(city.Country.CCA2))
                        {
                            var existingCountry = await _context.Countries.FindAsync(city.Country.CCA2);
                            if (existingCountry != null)
                            {
                                countryDict[city.Country.CCA2] = existingCountry;
                            }
                            else
                            {
                                _context.Attach(city.Country);
                                countryDict[city.Country.CCA2] = city.Country;
                            }
                        }
                        city.Country = countryDict[city.Country.CCA2];
                        cityDict[city.CityId] = city;
                        _context.Attach(city);
                    }
                }

                tourCities.Add(cityDict[city.CityId]);
            }

            Tour.Cities = tourCities;
            _context.AttachRange(Tour.Cities);

            var hotels = new List<Hotel>(Tour.Hotels);

            for (int i = 0; i < hotels.Count; i++)
            {
                var hotel = hotels[i];

                foreach (var review in hotel.Reviews.Where(review => string.IsNullOrEmpty(review.ReviewId)))
                {
                    review.ReviewId = Guid.NewGuid().ToString();
                }

                // Check if the hotel exists in the database
                var existingHotel = await _context.Hotels
                                                  .Include(h => h.City)
                                                  .ThenInclude(c => c.Country)
                                                  .FirstOrDefaultAsync(h => h.PlaceId == hotel.PlaceId);

                if (existingHotel != null)
                {
                    //// If the hotel exists, detach the existing hotel from the context
                    //_context.Entry(existingHotel).State = EntityState.Detached;

                    //if (cityDict.ContainsKey(hotel.City.CityId))
                    //    hotel.City = cityDict[hotel.City.CityId];
                    //else
                    //{
                    //    var existingCity = await _context.Cities
                    //                                     .Include(c => c.Country)
                    //                                     .FirstOrDefaultAsync(c => c.CityId == hotel.City.CityId);

                    //    if (existingCity != null)
                    //    {
                    //        cityDict[hotel.City.CityId] = existingCity;
                    //    }
                    //    else
                    //    {
                    //        if (!countryDict.ContainsKey(hotel.City.Country.CCA2))
                    //        {
                    //            var existingCountry = await _context.Countries.FirstOrDefaultAsync(c => c.CCA2 == hotel.City.Country.CCA2);
                    //            if (existingCountry != null)
                    //            {
                    //                hotel.City.Country = existingCountry;
                    //                countryDict[hotel.City.Country.CCA2] = existingCountry;
                    //            }
                    //            else
                    //            {
                    //                _context.Attach(hotel.City.Country);
                    //                countryDict[hotel.City.Country.CCA2] = hotel.City.Country;
                    //            }
                    //        }
                    //        hotel.City.Country = countryDict[hotel.City.Country.CCA2];
                    //        cityDict[hotel.City.CityId] = hotel.City;
                    //        _context.Attach(hotel.City);
                    //    }
                    //}

                    existingHotel.Reviews = new List<Review>(hotel.Reviews);
                    hotels[i] = existingHotel;

                    //if (_context.Entry(existingHotel).State == EntityState.Detached)
                    //{
                    //    _context.Attach(existingHotel);
                    //}
                }
                else
                {
                    if (!cityDict.ContainsKey(hotel.City.CityId))
                    {
                        var existingCity = await _context.Cities
                                                         .Include(c => c.Country)
                                                         .FirstOrDefaultAsync(c => c.CityId == hotel.City.CityId);

                        if (existingCity != null)
                        {
                            cityDict[hotel.City.CityId] = existingCity;
                        }
                        else
                        {
                            if (!countryDict.ContainsKey(hotel.City.Country.CCA2))
                            {
                                var existingCountry = await _context.Countries.FirstOrDefaultAsync(c => c.CCA2 == hotel.City.Country.CCA2);
                                if (existingCountry != null)
                                {
                                    hotel.City.Country = existingCountry;
                                    countryDict[hotel.City.Country.CCA2] = existingCountry;
                                }
                                else
                                {
                                    _context.Attach(hotel.City.Country);
                                    countryDict[hotel.City.Country.CCA2] = hotel.City.Country;
                                }
                            }
                            hotel.City.Country = countryDict[hotel.City.Country.CCA2];
                            cityDict[hotel.City.CityId] = hotel.City;
                            _context.Attach(hotel.City);
                        }
                    }
                    else
                    {
                        hotel.City = cityDict[hotel.City.CityId];
                    }

                    _context.Hotels.Add(hotel);
                }
            }

            Tour.Hotels = hotels;

            await _context.SaveChangesAsync();

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    string tourMark = Guid.NewGuid().ToString();

                    foreach (var hotelOffer in Tour.HotelsOffers)
                    {
                        hotelOffer.OfferId += "_" + tourMark;

                        var Room = hotelOffer.Room;
                        var PolicyDetails = hotelOffer.PolicyDetails;

                        hotelOffer.Room = null;
                        hotelOffer.PolicyDetails = null;

                        await _context.AddAsync(hotelOffer);
                        await _context.SaveChangesAsync();

                        hotelOffer.Room = Room;
                        hotelOffer.Room.OfferId = hotelOffer.OfferId;
                        await _context.AddAsync(hotelOffer.Room);

                        hotelOffer.PolicyDetails = PolicyDetails;
                        hotelOffer.PolicyDetails.OfferId = hotelOffer.OfferId;
                        var Guarantee = hotelOffer.PolicyDetails.Guarantee;
                        var Deposit = hotelOffer.PolicyDetails.Deposit;
                        var Prepay = hotelOffer.PolicyDetails.Prepay;
                        var HoldTime = hotelOffer.PolicyDetails.HoldTime;

                        hotelOffer.PolicyDetails.Guarantee = null;
                        hotelOffer.PolicyDetails.Deposit = null;
                        hotelOffer.PolicyDetails.Prepay = null;
                        hotelOffer.PolicyDetails.HoldTime = null;

                        await _context.AddAsync(hotelOffer.PolicyDetails);
                        await _context.SaveChangesAsync();
                        hotelOffer.PolicyDetails = await _context.PolicyDetails.FirstOrDefaultAsync(pd => pd.OfferId == hotelOffer.OfferId);

                        if (hotelOffer.PolicyDetails != null)
                        {
                            hotelOffer.PolicyDetails.Guarantee = Guarantee;
                            if (hotelOffer.PolicyDetails?.Guarantee != null)
                                hotelOffer.PolicyDetails.Guarantee.PolicyDetailsId = hotelOffer.PolicyDetails.PolicyDetailsId;

                            hotelOffer.PolicyDetails.Deposit = Deposit;
                            if (hotelOffer.PolicyDetails?.Deposit != null)
                                hotelOffer.PolicyDetails.Deposit.PolicyDetailsId = hotelOffer.PolicyDetails.PolicyDetailsId;

                            hotelOffer.PolicyDetails.Prepay = Prepay;
                            if (hotelOffer.PolicyDetails?.Prepay != null)
                                hotelOffer.PolicyDetails.Prepay.PolicyDetailsId = hotelOffer.PolicyDetails.PolicyDetailsId;

                            hotelOffer.PolicyDetails.HoldTime = HoldTime;
                            if (hotelOffer.PolicyDetails?.HoldTime != null)
                                hotelOffer.PolicyDetails.HoldTime.PolicyDetailsId = hotelOffer.PolicyDetails.PolicyDetailsId;

                            await _context.SaveChangesAsync();
                        }
                    }

                    await _context.SaveChangesAsync();

                    Tour.Program = Tour.Program.OrderBy(ac => ac.StartDate).ToList();
                    Tour.StartDate = Tour.Program.First().StartDate;
                    Tour.EndDate = Tour.Program.Last().EndDate;

                    foreach (var act in Tour.Program.Where(ac => ac.ActType == Activity.ActivityType.POI && string.IsNullOrEmpty(ac.ActivityId)))
                    {
                        act.ActivityId = Guid.NewGuid().ToString();
                    }
                    foreach (var trans in Tour.Program.Where(ac => ac.ActType == Activity.ActivityType.TRANS))
                    {
                        trans.ActivityId += "_" + Guid.NewGuid().ToString();
                    }

                    _context.Tours.Add(Tour);
                    await _context.SaveChangesAsync();

                    var requstedTour = await _context.Tours.FirstOrDefaultAsync(t => t.Agency.AgencyId == Tour.Agency.AgencyId && t.StartDate == Tour.StartDate && t.EndDate == Tour.EndDate);

                    if (requstedTour != null)
                    {
                        using HttpClient client = new();
                        var scheduleTasksRequest = await client.GetAsync(navigationManager.BaseUri + $"tour/shedule-tour-tasks/{requstedTour.TourId}");

                        if (!scheduleTasksRequest.IsSuccessStatusCode)
                            throw new Exception(await scheduleTasksRequest.Content.ReadAsStringAsync());
                    }

                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
        }

        public async Task SetHotelsAsync(City city, DateTime checkInDate, int radius = 5, string radiusUnit = "KM",
            string[]? amenities = null, string[]? ratings = null, DateTime? checkOutDate = null, int? roomQuantity = null, Language lang = Language.English)
        {
            // Validate the input values
            if (radius < 1 || radius > 30)
                throw new ArgumentOutOfRangeException(nameof(radius), $"Radius must be within the range of 1-30");
            if (checkOutDate != null && checkOutDate.Value < checkInDate.AddDays(1))
                throw new ArgumentOutOfRangeException(nameof(checkOutDate), $"Minimum check-out date is {checkInDate.AddDays(1):d}");
            if (roomQuantity < 1 || roomQuantity > 9)
                throw new ArgumentOutOfRangeException(nameof(roomQuantity), "Number of rooms must be within the range of 1-9");

            // Set the check-out date to the next day after the check-in date if it is not specified
            checkOutDate ??= checkInDate.AddDays(1);

            try
            {
                // Initialize hotel and offer lists
                Hotels = new();
                HotelsOffers = new();

                // Get hotels and their offers using the Amadeus API
                (Hotels, HotelsOffers) = await amadeusService.GetHotelsAsync(city, checkInDate, radius, radiusUnit, amenities, ratings, checkOutDate, roomQuantity, lang);

                // Sort hotel offers by the number of adults
                HotelsOffers = new(HotelsOffers.Select(hos => new KeyValuePair<string, List<HotelsOffer>>(hos.Key, hos.Value.OrderBy(o => o.AdultsQuantity).ToList())));

                // Split hotel IDs into chunks of 3 for parallel execution of requests to get hotel details
                var idChunks = Hotels.Keys.Chunk(size: 3);
                List<Task> detailsTasks = new();
                foreach (var chunk in idChunks)
                {
                    // Create tasks to get hotel details
                    detailsTasks.Add(Task.Run(async () =>
                    {
                        await SetPalcesDetailsAsync(chunk, PlaceLocationType.Lodging, lang);
                    }));
                }

                // Wait for all tasks to complete
                await Task.WhenAll(detailsTasks);
            }
            catch (Exception ex)
            {
                // Handle exceptions and print the error message to the console
                Console.WriteLine(ex.ToString());
            }
        }


        public async Task SetTransferOffers(Location startPlace, Location endPlace, DateTime startDateTime,
            int passengers, int? children = null, DateTime? endDateTime = null, Models.TransferOffer.TransferType? transferType = null,
            Models.TransferOffer.VehicleCategory? vehicleCategory = null, Models.TransferOffer.VehicleType? vehicleType = null, Language lang = Language.English)
        {
            if (startPlace is null || endPlace is null)
                throw new ArgumentNullException(startPlace is null ? nameof(startPlace) : nameof(endPlace), "Start/End place cannot be null");
            if (startPlace == endPlace)
                throw new ArgumentException("Start and end places cannot be the same", nameof(endPlace));
            if (passengers < 1 || passengers > Tour.ParticipantsMaxNumber)
                throw new ArgumentOutOfRangeException(nameof(passengers), $"Number of passengers must be within the range of 1-{Tour.ParticipantsMaxNumber}");
            if (children != null && (children > passengers - 1 || children < 0))
                throw new ArgumentOutOfRangeException(nameof(children), $"Number of children must be within the range of 0-{passengers - 1}");
            if (endDateTime != null && endDateTime! > startDateTime)
                throw new ArgumentOutOfRangeException(nameof(endDateTime), "End date cannot be earlier or equal to the start date");

            Activities = new();

            var offers = await amadeusService.GetTransferOffers(startPlace, endPlace, startDateTime, passengers, children, endDateTime, transferType,
                vehicleCategory, vehicleType, lang);

            foreach (var offer in offers)
            {
                Activities.TryAdd(offer.ActivityId, offer);
            }
        }

        public async Task SetPoIsAsync(double latitude, double longitude, int radius = 1, PointOfInteres.LocationCategory[]? categories = null, Language lang = Language.English)
        {
            // Check if the radius is within the range of 1 to 20
            if (radius < 1 || radius > 20)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be within the range of 1-20 inclusive");

            // Initialize the list of activities
            Activities = new();

            // Get the list of points of interest using the input parameters
            var pois = await amadeusService.GetPointsOfInterestAsync(latitude, longitude, radius, categories);

            // Add each point of interest to the Activities list
            foreach (var poi in pois)
            {
                Activities.TryAdd(poi.ActivityId, poi);
            }

            // Split the keys of Activities into groups of 3 and call SetPalcesDetailsAsync for each group asynchronously
            var idChunks = Activities.Keys.Chunk(size: 3);
            List<Task> detailsTasks = new();
            foreach (var chunk in idChunks)
            {
                detailsTasks.Add(Task.Run(async () =>
                {
                    await SetPalcesDetailsAsync(chunk, PlaceLocationType.Point_Of_Interest, lang);
                }));
            }

            // Wait for the completion of all asynchronous tasks
            await Task.WhenAll(detailsTasks);
        }


        public async Task SetSidesAsync(double latitude, double longitude, int radius = 1)
        {
            // Check if the radius is within the range of 1 to 20
            if (radius < 1 || radius > 20)
                throw new ArgumentOutOfRangeException(nameof(radius), "Radius must be within the range of 1-20 inclusive");

            // Initialize the list of activities
            Activities = new();

            // Get the list of sides using the input parameters
            var sides = await amadeusService.GetSidesAsync(latitude, longitude, radius);

            // Add each side to the Activities list
            foreach (var side in sides)
            {
                Activities.TryAdd(side.ActivityId, side);
            }
        }

        private async Task SetPalcesDetailsAsync(string[] Ids, PlaceLocationType placeType, Language lang)
        {
            foreach (var Id in Ids)
            {
                GooglePlace place;
                try
                {
                    switch (placeType)
                    {
                        case PlaceLocationType.Lodging:
                            {
                                place = Hotels[Id];
                                place = await GetPlaceByQuery(query: place.Name, retrieveDetails: true, fields: FieldTypes.Basic | FieldTypes.Contact | FieldTypes.Atmosphere, 
                                    includePhotos: true, lang: lang, latitude: place.Location.Latitude, longitude: place.Location.Longitude);
                                place.Location.PlaceId = place.PlaceId;
                                break;
                            }
                        case PlaceLocationType.Point_Of_Interest:
                            {
                                place = new();
                                if (Activities[Id] is PointOfInteres poi)
                                {
                                    place = await GetPlaceByQuery(query: poi.Name, retrieveDetails: true, includePhotos: true, lang: lang, latitude: place.Location.Latitude, longitude: place.Location.Longitude);
                                    break;
                                }
                                else
                                    throw new ArgumentException("Wrong Place type", nameof(placeType));
                            }
                        default:
                            throw new ArgumentException("Wrong Place type", nameof(placeType));
                    }
                }
                catch (HttpRequestException ex)
                {
                    continue;
                }

                switch (placeType)
                {
                    case PlaceLocationType.Lodging:
                        {
                            Hotels.AddOrUpdate(Id, new Models.Hotel(), (id, h) =>
                            {
                                h.PlaceId = place.PlaceId;
                                h.Name = place.Name;
                                h.Description = place.Description;
                                h.Location = place.Location;
                                h.IconUri = place.IconUri;
                                h.Rating = place.Rating;
                                //h.ImagesPaths = place.ImagesPaths;
                                h.UserRatingCount = place.UserRatingCount;
                                h.Contact = place.Contact;
                                h.WeekdayDescriptions = place.WeekdayDescriptions;
                                h.Reviews = place.Reviews;

                                return h;
                            });

                            break;
                        }
                    case PlaceLocationType.Point_Of_Interest:
                        {
                            Activities.AddOrUpdate(Id, new PointOfInteres(), (id, a) =>
                            {
                                if (a is PointOfInteres poi)
                                {
                                    poi.Name = place.Name;
                                    poi.PlaceIconUrl = place.IconUri;
                                    poi.Location = place.Location;
                                    return poi;
                                }
                                return a;
                            });

                            break;
                        }
                }
            }
        }

        public async Task<GooglePlace> GetPlaceByQuery(string query, bool retrieveDetails = false, FieldTypes fields = FieldTypes.Basic, bool includePhotos = false, Language lang = Language.English, 
            double? latitude = null, double? longitude = null)
        {
            GooglePlace place = new();

            PlacesTextSearchRequest searchRequest = new()
            {
                Key = googleApiKey,
                Query = query,
                Radius = 1000,
                Language = lang
            };
            if (latitude != null && longitude != null)
                searchRequest.Location = new(latitude.Value, longitude.Value);

            var searchResponse = await GoogleApi.GooglePlaces.Search.TextSearch.QueryAsync(searchRequest);

            if (searchResponse != null && searchResponse.Status == Status.Ok)
            {
                var result = searchResponse.Results.FirstOrDefault();
                if (result != null)
                {
                    place.PlaceId = result.PlaceId;
                    place.Name = result.Name;
                    place.Location.FormattedAddress = result.FormattedAddress;
                    place.Location.Latitude = result.Geometry.Location.Latitude;
                    place.Location.Longitude = result.Geometry.Location.Longitude;
                    place.IconUri = result.Icon;
                    place.Rating = result.Rating;
                    place.UserRatingCount = result.UserRatingsTotal;
                }
            }
            else
                throw new HttpRequestException("Incorrect search query");

            if (retrieveDetails)
            {
                PlacesDetailsRequest detailsRequest = new()
                {
                    Key = googleApiKey,
                    PlaceId = place.PlaceId,
                    Language = lang,
                    Fields = fields
                };
                var detailsResponse = await GoogleApi.GooglePlaces.Details.QueryAsync(detailsRequest);

                if (detailsResponse != null && detailsResponse.Status == Status.Ok)
                {
                    var result = detailsResponse.Result;
                    if (result != null)
                    {
                        place.Description = result.EditorialSummary?.Overview;
                        place.Location.ShortAddress = result.Vicinity;

                        List<AddressComponent> addressComponents = new();
                        foreach (var addressComponent in result.AddressComponents)
                        {
                            List<Models.AddressComponentType> types = new();
                            foreach(var addressComponentType in addressComponent.Types)
                            {
                                types.Add((Models.AddressComponentType)Enum.Parse(typeof(Models.AddressComponentType), addressComponentType.ToString()));
                            }

                            addressComponents.Add(new()
                            {
                                LongName = addressComponent.LongName,
                                ShortName = addressComponent.ShortName,
                                Types = types
                            });
                        }
                        place.Location.AddressComponents = addressComponents;
                        place.Location.UtcOffsetMinutes = result.UtcOffset;
                        place.Location.GoogleMapsUri = result.Url;
                        place.Contact ??= new();
                        place.Contact.PhoneNumber = result.InternationalPhoneNumber;
                        place.Contact.WebsiteUri = result.Website;
                        place.WeekdayDescriptions = result.OpeningHours?.WeekdayTexts?.ToList() ?? new();
                        place.Reviews = result.Reviews?.
                                            Select(r => new Review()
                                            {
                                                PublishTimeDescription = r.RelativeTime,
                                                Rating = r.Rating,
                                                Text = r.Text,
                                                AuthorDisplayName = r.AuthorName,
                                                PhotoUri = r.ProfilePhotoUrl
                                            })
                                            .ToList() ?? new();
                    }
                }
                else
                    throw new HttpRequestException("Incorrect details request");

                if (includePhotos)
                {
                    using HttpClient client = new();
                    string url = $"https://places.googleapis.com/v1/places/{place.PlaceId}?fields=photos&key={googleApiKey}";
                    var response = await client.GetAsync(url);
                    if (response != null && response.IsSuccessStatusCode)
                    {
                        JObject body = JObject.Parse(await response.Content.ReadAsStringAsync());

                        if (body.ContainsKey("photos"))
                        {
                            foreach (var photo in body["photos"])
                            {
                                string photoUrl = $"https://places.googleapis.com/v1/{photo["name"]}/media?key={googleApiKey}&skipHttpRedirect=true&maxHeightPx=300";

                                using HttpClient photoClient = new();
                                var photoResponse = await photoClient.GetAsync(photoUrl);
                                if (photoResponse != null && photoResponse.IsSuccessStatusCode)
                                {
                                    JToken photoObj = JToken.Parse(await photoResponse.Content.ReadAsStringAsync());
                                    place.Location.ImagesPaths.Add(photoObj["photoUri"]?.ToString() ?? string.Empty);
                                }
                            }
                        }
                    }
                }
            }

            return place;
        }
    }
}
