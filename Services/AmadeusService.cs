using amadeus;
using amadeus.exceptions;
using amadeus.resources;
using GoogleApi.Entities.Common.Enums;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Travel_App_Web;
using static amadeus.resources.HotelOffer;
using static Models.HotelsOffer;
using static Models.PointOfInteres;
using static Models.TransferOffer;

namespace Services
{
    public class AmadeusService
    {
        private ConcurrentDictionary<string, Models.Hotel> _hotels = new();
        private ConcurrentDictionary<string, List<HotelsOffer>> _hotelsOffers = new();
        private string? bearerToken;
        private HttpClient? amadeusClient;
        private IConfiguration configuration;

        public AmadeusService(IConfiguration _configuration) 
        {
            configuration = _configuration;
            SetClient();
        }
        private void SetClient()
        {
            string apiKey = configuration["AMADEUS_CLIENT_ID"];
            string apiSecret = configuration["AMADEUS_CLIENT_SECRET"];

            var message = new HttpRequestMessage(HttpMethod.Post, "https://test.api.amadeus.com/v1/security/oauth2/token")
            {
                Content = new StringContent(
                $"grant_type=client_credentials&client_id={apiKey}&client_secret={apiSecret}",
                Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            using HttpClient http = new();
            var results = http.Send(message);
            string resultStr = results.Content.ReadAsStringAsync().Result;
            JToken authResults = JToken.Parse(resultStr);

            bearerToken = authResults["access_token"].ToString();

            amadeusClient = new()
            {
                BaseAddress = new Uri("https://test.api.amadeus.com"),
            };
            amadeusClient.DefaultRequestHeaders.Accept.Clear();
            amadeusClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.amadeus+json"));
            amadeusClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
        }

        #region Hotel Functions
        public async Task<(ConcurrentDictionary<string, Models.Hotel>, Dictionary<string, List<HotelsOffer>>)> GetHotelsAsync(City city, DateTime checkInDate, int radius = 5,
            string radiusUnit = "KM", string[]? amenities = null, string[]? ratings = null, DateTime? checkOutDate = null,
            int? roomQuantity = null, Language lang = Language.English)
        {
            if (radius < 1 || radius > 30)
                throw new ArgumentOutOfRangeException(nameof(radius), $"Radius must be between 1 and 30 inclusive");
            if (checkOutDate != null && checkOutDate.Value < checkInDate.AddDays(1))
                throw new ArgumentOutOfRangeException(nameof(checkOutDate), $"The earliest check-out date is {checkInDate.AddDays(1):d}");
            if (roomQuantity < 1 || roomQuantity > 9)
                throw new ArgumentOutOfRangeException(nameof(roomQuantity), "The number of rooms must be between 1 and 9 inclusive");
            checkOutDate ??= checkInDate.AddDays(1);
            _hotels = new();

            try
            {
                string url = string.Empty;
                Params @params;

                var culture = new CultureInfo("en-US");
                url = "v1/reference-data/locations/hotels/by-geocode";
                @params = Params.
                    with("latitude", $"{city.GeoCode["latitude"].ToString("G", culture)}").
                    and("longitude", $"{city.GeoCode["longitude"].ToString("G", culture)}");

                @params.and("radius", $"{radius}").
                    and("radiusUnit", $"{radiusUnit}");
                if (amenities != null && amenities.Length > 0)
                {
                    @params.and("amenities", $"{string.Join(",", amenities)}");
                }
                if (ratings != null && ratings.Length > 0)
                {
                    @params.and("ratings", $"{string.Join(",", ratings)}");
                }

                await Task.Run(async () =>
                {
                    string queryString = string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"));
                    string requestUrl = $"{url}?{queryString}";

                    HttpResponseMessage hotelResponse = await amadeusClient.GetAsync(requestUrl);

                    var hotelsResp = await hotelResponse.Content.ReadAsStringAsync();
                    JObject hotelsData = JObject.Parse(hotelsResp);

                    if (!hotelsData.ContainsKey("data"))
                    {
                        if (hotelsData.ContainsKey("errors") && int.Parse(hotelsData["errors"][0]["code"].ToString()) == 38192)
                        {
                            SetClient();
                            await GetHotelsAsync(city, checkInDate, radius, radiusUnit, amenities, ratings,
                                checkOutDate, roomQuantity, lang);
                        }
                        else
                            throw new Exception("An error occurred, please try again later");
                    }

                    foreach (var hotelData in hotelsData["data"])
                    {
                        var hotelId = hotelData["hotelId"].ToString();
                        var name = hotelData["name"].ToString();
                        string? starRating = starRating = hotelData["rating"]?.ToString();

                        var geoCodeJson = hotelData["geoCode"].ToString();
                        var geoCode = JsonConvert.DeserializeObject<Dictionary<string, double>>(geoCodeJson);

                        Models.Hotel hotel = new()
                        {
                            HotelId = hotelId,
                            Name = name,
                            City = city,
                            Location = new()
                            {
                                Latitude = geoCode["latitude"],
                                Longitude = geoCode["longitude"]
                            },
                            Rating = !string.IsNullOrEmpty(starRating) ? double.Parse(starRating) : null
                        };

                        _hotels.TryAdd(hotel.HotelId, hotel);
                    }
                });

                string[] hotelsIdsArr = _hotels.Select(h => h.Key).ToArray();
                string offsetString = city.Country.Timezones.FirstOrDefault();
                int offsetInt = Convert.ToInt32(offsetString[3..6]);
                TimeSpan offset = TimeSpan.FromHours(offsetInt);
                TimeZoneInfo timeZone = TimeZoneInfo.CreateCustomTimeZone("Custom", offset, "Custom", "Custom");
                List<Task> hotelsTasks = new();

                for (int i = 1; i <= 3; i++)
                {
                    int adults = i;

                    hotelsTasks.Add(Task.Run(async () =>
                    {
                        Dictionary<string, List<HotelsOffer>> hotelsOffers = await GetHotelsOffersAsync(hotelsIdsArr, timeZone, checkInDate, checkOutDate, roomQuantity, lang, adults);

                        foreach (var hotelsOffer in hotelsOffers)
                        {
                            if (hotelsOffer.Value.Count > 0)
                            {
                                _hotelsOffers.AddOrUpdate(hotelsOffer.Key, hotelsOffer.Value, (id, offers) =>
                                {
                                    offers.AddRange(hotelsOffer.Value);
                                    return offers;
                                });
                            }
                        }
                    }));
                }

                await Task.WhenAll(hotelsTasks);

                var hotelsToRemove = _hotels.Where(h => !_hotelsOffers.Any(ho => h.Key == ho.Key)).Select(h => h.Key);
                foreach (var h in hotelsToRemove)
                {
                    _hotels.Remove(h, out Models.Hotel? value);
                }

                return (_hotels, new(_hotelsOffers));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
        private async Task SetRatingsAsync(string[] hotelsIds)
        {
            if (hotelsIds.Length > 3)
            {
                await SetRatingsAsync(hotelsIds[..3]);
                await SetRatingsAsync(hotelsIds[3..]);
                return;
            }

            HttpResponseMessage ratingsResp = new();
            try
            {
                string queryString = $"hotelIds={string.Join(",", hotelsIds)}";
                string requestUrl = $"v2/e-reputation/hotel-sentiments?{queryString}";
                ratingsResp = await amadeusClient.GetAsync(requestUrl);
                if (ratingsResp.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                    throw new ServerException(new());
            }
            catch (Exception ex) when (ex is ServerException || ex is ClientException)
            {
                return;
            }

            string ratingsStr = await ratingsResp.Content.ReadAsStringAsync();
            JObject ratingsData = JObject.Parse(ratingsStr);

            if (!ratingsData.ContainsKey("data"))
                return;

            foreach (var ratingData in ratingsData["data"])
            {
                var hotelId = ratingData["hotelId"].ToString();
                var overallRating = ratingData["overallRating"].ToString();
                var sentimentsJson = ratingData["sentiments"].ToString();
                var sentiments = JsonConvert.DeserializeObject<Dictionary<string, int>>(sentimentsJson);

                _hotels.AddOrUpdate(hotelId, new Models.Hotel(), (id, hotel) =>
                {
                    hotel.Rating = double.Parse(overallRating) / 20.0;
                    hotel.Sentiments = sentiments;

                    return hotel;
                });
            }
        }
        #endregion

        #region Hotels Offers
        public async Task<Dictionary<string, List<HotelsOffer>>> GetHotelsOffersAsync(string[] hotelsIds, TimeZoneInfo timeZone, DateTime checkInDate,
            DateTime? checkOutDate = null, int? roomQuantity = null, Language lang = Language.English, int adults = 1)
        {
            if (hotelsIds.Length == 0)
                throw new ArgumentException("The array must contain at least one value", nameof(hotelsIds));
            if (checkOutDate != null && checkOutDate.Value < checkInDate.AddDays(1))
                throw new ArgumentOutOfRangeException(nameof(checkOutDate), $"The earliest acceptable check-out date is {checkInDate.AddDays(1):d}");
            if (roomQuantity < 1 || roomQuantity > 9)
                throw new ArgumentOutOfRangeException(nameof(roomQuantity), "The number of rooms must be between 1 and 9");
            if (adults < 1 || adults > 9)
                throw new ArgumentOutOfRangeException(nameof(adults), "The number of adults must be between 1 and 9");
            checkOutDate ??= checkInDate.AddDays(1);

            Dictionary<string, List<HotelsOffer>> hotelsOffers = new();

            if (hotelsIds.Length > 100)
            {
                var extendedIds = hotelsIds[100..];
                var extendedOffers = await GetHotelsOffersAsync(extendedIds, timeZone, checkInDate,
                    checkOutDate, roomQuantity, lang, adults);
                foreach (var hotelsOffer in extendedOffers)
                {
                    if (!hotelsOffers.TryAdd(hotelsOffer.Key, hotelsOffer.Value))
                        hotelsOffers[hotelsOffer.Key].AddRange(hotelsOffer.Value);
                }
                hotelsIds = hotelsIds[..100];
            }

            var @params = Params.
                with("hotelIds", $"{string.Join(",", hotelsIds)}").
                and("adults", $"{adults}").
                and("checkInDate", $"{checkInDate:yyyy-MM-dd}").
                and("checkOutDate", $"{checkOutDate.Value:yyyy-MM-dd}").
                and("lang", $"{lang.GetEnumMemberValue()}");
            if (roomQuantity != null)
            {
                @params.and("roomQuantity", $"{roomQuantity}");
            }


            string queryString = string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"));
            string requestUrl = $"v3/shopping/hotel-offers?{queryString}";

            HttpResponseMessage hotelsOffersResponse = await amadeusClient.GetAsync(requestUrl);

            if (hotelsOffersResponse.IsSuccessStatusCode)
            {
                var hotelOffersResp = await hotelsOffersResponse.Content.ReadAsStringAsync();

                JObject offersData = JObject.Parse(hotelOffersResp);

                if (!offersData.ContainsKey("data"))
                {
                    if (offersData.ContainsKey("errors") && int.Parse(offersData["errors"][0]["code"].ToString()) == 38192)
                    {
                        SetClient();
                        return await GetHotelsOffersAsync(hotelsIds, timeZone, checkInDate, checkOutDate, roomQuantity, lang, adults);
                    }
                    else
                        throw new Exception("An error occurred, please try again later");
                }

                await Task.Run(() =>
                {
                    foreach (var offerData in offersData["data"])
                    {
                        bool available = bool.Parse(offerData["available"].ToString());
                        if (!available)
                            continue;

                        var hotelId = offerData["hotel"]["hotelId"].ToString();

                        List<HotelsOffer> hotelOffers = new();

                        JArray offers = JArray.Parse(offerData["offers"].ToString());

                        foreach (var offer in offers)
                        {
                            try
                            {
                                var idInput = offer["id"].ToString();
                                var checkInDateInput = DateTime.Parse(offer["checkInDate"].ToString());
                                var checkOutDateInput = DateTime.Parse(offer["checkOutDate"].ToString());
                                int? roomQuantityInput = int.Parse(offer["roomQuantity"]?.ToString() ?? "1");

                                var rateCodeInputStr = offer["rateCode"]?.ToString();
                                SpecialRate? rateCodeInput = null;
                                if (rateCodeInputStr != null)
                                {
                                    try
                                    {
                                        rateCodeInput = (SpecialRate)Enum.Parse(typeof(SpecialRate), rateCodeInputStr);
                                    }
                                    catch
                                    {
                                        // Will not add non-existent special rate code
                                    }
                                }

                                var adultsInput = int.Parse(offer["guests"]["adults"].ToString());
                                var descriptionInput = offer["description"]?["text"]?.ToString();

                                List<BoardType>? boardTypeInput = null;
                                var boardTypeStrInput = offer["boardType"]?.ToString();
                                if (boardTypeStrInput != null)
                                {
                                    var boardTypeListInput = boardTypeStrInput.Split(",");
                                    if (boardTypeListInput != null && boardTypeListInput.Length > 0)
                                    {
                                        foreach (var boardTypeStr in boardTypeListInput)
                                        {
                                            boardTypeInput ??= new();
                                            try
                                            {
                                                boardTypeInput.Add((BoardType)Enum.Parse(typeof(BoardType), boardTypeStr));
                                            }
                                            catch
                                            {
                                                // Will not add non-existent board type code
                                            }
                                        }
                                    }
                                }

                                RoomDetails roomInput = JsonConvert.DeserializeObject<RoomDetails>(offer["room"].ToString());

                                HotelPrice priceInput = JsonConvert.DeserializeObject<HotelPrice>(offer["price"].ToString());

                                HotelOffer.PolicyDetails policiesInput = new();
                                if (offer["policies"] != null)
                                {
                                    policiesInput = JsonConvert.DeserializeObject<HotelOffer.PolicyDetails>(offer["policies"].ToString());
                                }
                                var paymentTypeInput = offer["policies"]?["paymentType"]?.ToString();

                                var cancellationsStrInput = offer["policies"]?["cancellations"]?.ToString();
                                List<CancellationPolicy>? cancellationsInput = null;
                                if (cancellationsStrInput != null)
                                {
                                    cancellationsInput = JsonConvert.DeserializeObject<List<CancellationPolicy>>(cancellationsStrInput);
                                }

                                var checkInOutPolicyStrInput = offer["policies"]?["checkInOut"]?.ToString();
                                HotelOffer.CheckInOutPolicy? checkInOutPolicyInput = null;
                                if (checkInOutPolicyStrInput != null)
                                {
                                    checkInOutPolicyInput = JsonConvert.DeserializeObject<HotelOffer.CheckInOutPolicy>(checkInOutPolicyStrInput);
                                }

                                if (policiesInput.guarantee != null && policiesInput.guarantee.acceptedPayments != null)
                                {
                                    policiesInput.guarantee.acceptedPayments.method = offer["policies"]["guarantee"]["acceptedPayments"]?["methods"]?.ToString();
                                }
                                if (policiesInput.deposit != null && policiesInput.deposit.acceptedPayments != null)
                                {
                                    policiesInput.deposit.acceptedPayments.method = offer["policies"]["deposit"]["acceptedPayments"]?["methods"]?.ToString();
                                }
                                if (policiesInput.prepay != null && policiesInput.prepay.acceptedPayments != null)
                                {
                                    policiesInput.prepay.acceptedPayments.method = offer["policies"]["prepay"]["acceptedPayments"]?["methods"]?.ToString();
                                }
                                if (policiesInput.holdTime != null && policiesInput.holdTime.acceptedPayments != null)
                                {
                                    policiesInput.holdTime.acceptedPayments.method = offer["policies"]["holdTime"]["acceptedPayments"]?["methods"]?.ToString();
                                }

                                Models.PolicyDetails policyDetails = new();
                                if (policiesInput.guarantee != null)
                                {
                                    DateTime? deadLineLoc = null;
                                    if (policiesInput.guarantee.deadline != null)
                                    {
                                        deadLineLoc = DateTime.Parse(policiesInput.guarantee.deadline);
                                    }

                                    List<VendorCodes>? creditCards = null;
                                    if (policiesInput.guarantee.acceptedPayments != null)
                                    {
                                        foreach (var creditCardStr in policiesInput.guarantee.acceptedPayments.creditCards)
                                        {
                                            creditCards ??= new();
                                            try
                                            {
                                                creditCards.Add((VendorCodes)Enum.Parse(typeof(VendorCodes), creditCardStr));
                                            }
                                            catch
                                            {
                                                // Will not add non-existent vendor code
                                            }
                                        }
                                    }

                                    policyDetails.Guarantee = new()
                                    {
                                        Amount = double.Parse(policiesInput.guarantee.amount ?? "-1"),
                                        Deadline = deadLineLoc,
                                        Description = policiesInput.guarantee.description?.text,
                                        CreditCards = creditCards,
                                        Methods = JsonConvert.DeserializeObject<List<string>>(policiesInput.guarantee.acceptedPayments?.method ?? "[]")
                                    };
                                    if (policyDetails.Guarantee.Amount == -1)
                                    {
                                        policyDetails.Guarantee.Amount = null;
                                    }
                                    if (policyDetails.Guarantee.Methods?.Count == 0)
                                    {
                                        policyDetails.Guarantee.Methods = null;
                                    }
                                }
                                if (policiesInput.deposit != null)
                                {
                                    DateTime? deadLineLoc = null;
                                    if (policiesInput.deposit.deadline != null)
                                    {
                                        deadLineLoc = DateTime.Parse(policiesInput.deposit.deadline);
                                    }

                                    List<VendorCodes>? creditCards = null;
                                    if (policiesInput.deposit.acceptedPayments != null)
                                    {
                                        foreach (var creditCardStr in policiesInput.deposit.acceptedPayments.creditCards)
                                        {
                                            creditCards ??= new();
                                            try
                                            {
                                                creditCards.Add((VendorCodes)Enum.Parse(typeof(VendorCodes), creditCardStr));
                                            }
                                            catch
                                            {
                                                // Will not add non-existent vendor code
                                            }
                                        }
                                    }

                                    policyDetails.Deposit = new()
                                    {
                                        Amount = double.Parse(policiesInput.deposit.amount ?? "-1"),
                                        Deadline = deadLineLoc,
                                        Description = policiesInput.deposit.description?.text,
                                        CreditCards = creditCards,
                                        Methods = JsonConvert.DeserializeObject<List<string>>(policiesInput.deposit.acceptedPayments?.method ?? "[]")
                                    };
                                    if (policyDetails.Deposit.Amount == -1)
                                    {
                                        policyDetails.Deposit.Amount = null;
                                    }
                                    if (policyDetails.Deposit.Methods?.Count == 0)
                                    {
                                        policyDetails.Deposit.Methods = null;
                                    }
                                }
                                if (policiesInput.prepay != null)
                                {
                                    DateTime? deadLineLoc = null;
                                    if (policiesInput.prepay.deadline != null)
                                    {
                                        deadLineLoc = DateTime.Parse(policiesInput.prepay.deadline);
                                    }

                                    List<VendorCodes>? creditCards = null;
                                    if (policiesInput.prepay.acceptedPayments != null)
                                    {
                                        foreach (var creditCardStr in policiesInput.prepay.acceptedPayments.creditCards)
                                        {
                                            creditCards ??= new();
                                            try
                                            {
                                                creditCards.Add((VendorCodes)Enum.Parse(typeof(VendorCodes), creditCardStr));
                                            }
                                            catch
                                            {
                                                // Will not add non-existent vendor code
                                            }
                                        }
                                    }

                                    policyDetails.Prepay = new()
                                    {
                                        Amount = double.Parse(policiesInput.prepay.amount ?? "-1"),
                                        Deadline = deadLineLoc,
                                        Description = policiesInput.prepay.description.text,
                                        CreditCards = creditCards,
                                        Methods = JsonConvert.DeserializeObject<List<string>>(policiesInput.prepay.acceptedPayments?.method ?? "[]")
                                    };
                                    if (policyDetails.Prepay.Amount == -1)
                                    {
                                        policyDetails.Prepay.Amount = null;
                                    }
                                    if (policyDetails.Prepay.Methods?.Count == 0)
                                    {
                                        policyDetails.Prepay.Methods = null;
                                    }
                                }
                                if (policiesInput.holdTime != null)
                                {
                                    DateTime? deadLineLoc = null;
                                    if (policiesInput.holdTime.deadline != null)
                                    {
                                        deadLineLoc = DateTime.Parse(policiesInput.holdTime.deadline);
                                    }

                                    List<VendorCodes>? creditCards = null;
                                    if (policiesInput.holdTime.acceptedPayments != null)
                                    {
                                        foreach (var creditCardStr in policiesInput.holdTime.acceptedPayments.creditCards)
                                        {
                                            creditCards ??= new();
                                            try
                                            {
                                                creditCards.Add((VendorCodes)Enum.Parse(typeof(VendorCodes), creditCardStr));
                                            }
                                            catch
                                            {
                                                // Will not add non-existent vendor code
                                            }
                                        }
                                    }

                                    policyDetails.HoldTime = new()
                                    {
                                        Amount = double.Parse(policiesInput.holdTime.amount ?? "-1"),
                                        Deadline = deadLineLoc,
                                        Description = policiesInput.holdTime.description?.text,
                                        CreditCards = creditCards,
                                        Methods = JsonConvert.DeserializeObject<List<string>>(policiesInput.holdTime.acceptedPayments?.method ?? "[]")
                                    };
                                    if (policyDetails.HoldTime.Amount == -1)
                                    {
                                        policyDetails.HoldTime.Amount = null;
                                    }
                                    if (policyDetails.HoldTime.Methods?.Count == 0)
                                    {
                                        policyDetails.HoldTime.Methods = null;
                                    }
                                }
                                if (cancellationsInput != null && cancellationsInput.Count > 0)
                                {
                                    policyDetails.Cancellations = new();
                                    foreach (var cancel in cancellationsInput)
                                    {
                                        Models.Cancellation cancellation = new()
                                        {
                                            Type = cancel.type,
                                            Description = cancel.description?.text,
                                            Amount = double.Parse(cancel.amount ?? "-1", CultureInfo.InvariantCulture),
                                            NumberOfNights = cancel.numberOfNights,
                                            Percentage = double.Parse(cancel.percentage ?? "-1", CultureInfo.InvariantCulture),
                                            Deadline = DateTime.Parse(cancel.deadline ?? DateTime.Today.AddDays(-10).ToString())
                                        };
                                        if (cancellation.Amount == -1)
                                        {
                                            cancellation.Amount = null;
                                        }
                                        if (cancellation.Percentage == -1)
                                        {
                                            cancellation.Percentage = null;
                                        }
                                        if (cancellation.Deadline == DateTime.Today.AddDays(-10))
                                        {
                                            cancellation.Deadline = null;
                                        }
                                        //else
                                        //{
                                        //    cancellation.Deadline = TimeZoneInfo.ConvertTimeToUtc(cancellation.Deadline.Value, timeZone);
                                        //}
                                        policyDetails.Cancellations.Add(cancellation);
                                    }
                                }
                                if (checkInOutPolicyInput != null)
                                {
                                    policyDetails.CheckInOut = new()
                                    {
                                        CheckIn = checkInOutPolicyInput.checkIn,
                                        CheckInDescription = checkInOutPolicyInput.checkInDescription.text,
                                        CheckOut = checkInOutPolicyInput.checkOut,
                                        CheckOutDescription = checkInOutPolicyInput.checkOutDescription.text
                                    };
                                }

                                HotelsOffer hotelOffer = new()
                                {
                                    OfferId = idInput,
                                    HotelId = hotelId,
                                    CheckInDate = checkInDateInput,
                                    CheckOutDate = checkOutDateInput,
                                    RoomQuantity = roomQuantityInput,
                                    AdultsQuantity = adultsInput,
                                    Description = descriptionInput,
                                    RateCode = rateCodeInput,
                                    Boards = boardTypeInput,
                                    Room = new()
                                    {
                                        Type = roomInput?.type ?? string.Empty,
                                        Category = roomInput?.typeEstimated?.category,
                                        Beds = roomInput?.typeEstimated?.beds,
                                        BedType = roomInput?.typeEstimated?.bedType,
                                        Description = roomInput?.description?.text
                                    },
                                    PriceTotal = decimal.Parse(priceInput.total, CultureInfo.InvariantCulture),
                                    Currency = priceInput.currency,
                                    PaymentType = paymentTypeInput,
                                    PolicyDetails = policyDetails
                                };

                                hotelOffers.Add(hotelOffer);
                            }
                            catch (Exception ex)
                            {
                                string mes = ex.ToString();
                            }
                        }

                        hotelsOffers.TryAdd(hotelId, hotelOffers);
                    }
                });
            }
            else
            {
                var status = hotelsOffersResponse.StatusCode;
            }

            return hotelsOffers;
        }
        #endregion

        #region Hotel Order

        public async Task<List<HotelOrder>> BookHotels(ParticipantUnit participantUnit, List<HotelsOffer> hotelsOffers)
        {
            if (string.IsNullOrEmpty(participantUnit.PrimaryUser.Email))
                throw new ArgumentException("Check the details of the primary participant", nameof(participantUnit.PrimaryUser.Email));

            if (participantUnit.OtherUsers.Any(op =>
            {
                if (op.User != null)
                {
                    return string.IsNullOrEmpty(op.User.Email)
                        || string.IsNullOrEmpty(op.User.LastName)
                        || string.IsNullOrEmpty(op.User.FirstName)
                        || string.IsNullOrEmpty(op.User.Phone);
                }
                else
                {
                    return string.IsNullOrEmpty(op.Email)
                        || string.IsNullOrEmpty(op.LastName)
                        || string.IsNullOrEmpty(op.FirstName)
                        || string.IsNullOrEmpty(op.Phone);
                }
            }))
                throw new ArgumentException("Check the details of other participants");

            if (string.IsNullOrEmpty(participantUnit.CreditCard.Number))
                throw new ArgumentException("Check the details of the credit card", nameof(participantUnit.CreditCard));

            if (hotelsOffers.Count < 1)
                throw new ArgumentException("No orders available", nameof(hotelsOffers));


            ConcurrentBag<HotelOrder> orders = new();

            List<Task> requestTasks = new();
            foreach (var offer in hotelsOffers)
            {
                var currentOffer = offer;

                requestTasks.Add(Task.Run(async () =>
                {
                    var requestBody = new Dictionary<string, object>
                    {
                        ["data"] = new Dictionary<string, object>
                        {
                            ["offerId"] = currentOffer.OfferId,
                            ["guests"] = new List<Dictionary<string, object>>()
                            {
                                new()
                                {
                                    ["name"] = new Dictionary<string, object>
                                    {
                                        ["title"] = participantUnit.PrimaryUser.Gender == Gender.FL ? "MS" : "MR",
                                        ["firstName"] = participantUnit.PrimaryUser.FirstName.ToUpper(),
                                        ["lastName"] = participantUnit.PrimaryUser.LastName.ToUpper()
                                    },
                                    ["contact"] = new Dictionary<string, object>
                                    {
                                        ["phone"] = participantUnit.PrimaryUser.Phone,
                                        ["email"] = participantUnit.PrimaryUser.Email
                                    }
                                }
                            },
                            ["payments"] = new List<Dictionary<string, object>>
                            {
                                new Dictionary<string, object>
                                {
                                    ["method"] = "creditCard",
                                    ["card"] = new Dictionary<string, object>
                                    {
                                        ["vendorCode"] = participantUnit.CreditCard.VendorCode.ToString(),
                                        ["cardNumber"] = participantUnit.CreditCard.Number.Replace(" ", ""),
                                        ["expiryDate"] = participantUnit.CreditCard.ExpiryDate.ToString("yyyy-MM")
                                    }
                                }
                            }
                        }
                    };
                    string jsonRequestBody = JsonConvert.SerializeObject(requestBody);

                    try
                    {
                        foreach (var result in await InnerBookingMethod(currentOffer, jsonRequestBody))
                        {
                            orders.Add(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        string mess = ex.Message;
                        throw;
                    }
                }));
            }

            await Task.WhenAll(requestTasks);

            return new(orders);
        }

        private async Task<List<HotelOrder>> InnerBookingMethod(HotelsOffer hotelsOffer, string requestBody)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.amadeus+json"));
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
            HttpResponseMessage ordersResponse =
            await client.PostAsJsonAsync("https://test.api.amadeus.com/v1/booking/hotel-bookings", requestBody);

            List<HotelOrder> results = new();

            if (ordersResponse.IsSuccessStatusCode)
            {
                string responseContent = await ordersResponse.Content.ReadAsStringAsync();
                JObject transfersData = JObject.Parse(responseContent);

                if (!transfersData.ContainsKey("data"))
                {
                    if (transfersData.ContainsKey("errors") && int.Parse(transfersData["errors"][0]["code"].ToString()) == 38192)
                    {
                        SetClient();
                        return await InnerBookingMethod(hotelsOffer, requestBody);
                    }
                    else
                        throw new Exception("An error occurred, please try again later");
                }

                foreach (var orderData in transfersData["data"])
                {
                    string orderId = orderData["id"]?.ToString() ?? string.Empty;
                    string providerConfirmationId = orderData["providerConfirmationId"]?.ToString() ?? string.Empty;

                    HotelOrder order = new()
                    {
                        OrderId = orderId,
                        ProviderConfirmationId = providerConfirmationId,
                        HotelsOffer = hotelsOffer
                    };

                    results.Add(order);
                }

                return results;
            }
            else
            {
                string response = string.Empty;
                string responseContent = await ordersResponse.Content.ReadAsStringAsync();
                JObject transfersData = JObject.Parse(responseContent);
                if (Convert.ToInt32(transfersData["code"].ToString()) == 33553)
                    response = "Order ID has expired, please update your orders and try again";

                throw new Exception(!string.IsNullOrEmpty(response) ? response : $"Bad response from \"booking/hotel-bookings\". {responseContent}");
            }
        }

        #endregion

        #region Transfer

        public async Task<List<TransferOffer>> GetTransferOffers(Models.Location startPlace, Models.Location endPlace, DateTime startDateTime,
            int passengers, int? children = null, DateTime? endDateTime = null, TransferType? transferType = null,
            VehicleCategory? vehicleCategory = null, VehicleType? vehicleType = null, Language lang = Language.English)
        {
            if (startPlace is null || endPlace is null)
                throw new ArgumentNullException(startPlace is null ? nameof(startPlace) : nameof(endPlace), "Departure/arrival place cannot be null");
            if (startPlace == endPlace)
                throw new ArgumentException("Departure and arrival places are the same", nameof(endPlace));
            if (passengers < 1)
                throw new ArgumentOutOfRangeException(nameof(passengers));
            if (children != null && (children > passengers - 1 || children < 0))
                throw new ArgumentOutOfRangeException(nameof(children), $"Number of children must be between 0 and {passengers - 1}");
            if (endDateTime != null && endDateTime! > startDateTime)
                throw new ArgumentOutOfRangeException(nameof(endDateTime), "Arrival date cannot be earlier than or equal to the departure date");

            var culture = new CultureInfo("en-US");
            var requestBody = new Dictionary<string, object>
            {
                { "startGooglePlaceId", $"{startPlace.PlaceId}" },
                { "startAddressLine", $"" },

                { "startZipCode", $"{startPlace.AddressComponents.
                            Find(c => c.Types.Any(t => t == Models.AddressComponentType.Postal_Code))?.ShortName}" },

                { "startCountryCode", $"{startPlace.AddressComponents.
                            Find(c => c.Types.Any(t => t == Models.AddressComponentType.Country))?.ShortName}" },

                { "startCityName", $"{startPlace.AddressComponents.
                            Find(c => c.Types.Any(t => t == Models.AddressComponentType.Locality))?.ShortName}" },

                { "startGeoCode", $"{startPlace.Latitude.ToString("G", culture)},{startPlace.Longitude.ToString("G", culture)}" },

                { "endGooglePlaceId", $"{endPlace.PlaceId}" },
                { "endAddressLine", $"" },
                { "endZipCode", $"{endPlace.AddressComponents.
                            Find(c => c.Types.Any(t => t == Models.AddressComponentType.Postal_Code))?.ShortName}" },

                { "endCountryCode", $"{endPlace.AddressComponents.
                            Find(c => c.Types.Any(t => t == Models.AddressComponentType.Country))?.ShortName}" },

                { "endCityName", $"{endPlace.AddressComponents.
                            Find(c => c.Types.Any(t => t == Models.AddressComponentType.Locality))?.ShortName}" },

                { "endGeoCode", $"{endPlace.Latitude.ToString("G", culture)},{endPlace.Longitude.ToString("G", culture)}" },

                { "startDateTime", $"{startDateTime:s}" },
                { "passengers", passengers },
                { "language", lang.GetEnumMemberValue() }
            };

            try
            {
                string? startAddressLine = startPlace.ShortAddress;
                if (string.IsNullOrEmpty(startAddressLine))
                {
                    startAddressLine = string.Join(' ', startPlace.AddressComponents.
                        Where(c => c.Types.
                            Any(t => t == Models.AddressComponentType.Street_Number
                                || t == Models.AddressComponentType.Route)).
                        Select(c => c.ShortName));
                }
                if (string.IsNullOrEmpty(startAddressLine))
                {
                    startAddressLine = startPlace.FormattedAddress.TakeWhile(c => c != ',').ToString();
                }
                if (string.IsNullOrEmpty(startAddressLine))
                    throw new ArgumentException("Cannot calculate start address line", nameof(startPlace));
                requestBody["startAddressLine"] = startAddressLine;

                string? endAddressLine = endPlace.ShortAddress;
                if (string.IsNullOrEmpty(endAddressLine))
                {
                    endAddressLine = string.Join(' ', endPlace.AddressComponents.
                        Where(c => c.Types.
                            Any(t => t == Models.AddressComponentType.Street_Number
                                || t == Models.AddressComponentType.Route)).
                        Select(c => c.ShortName));
                }
                if (string.IsNullOrEmpty(endAddressLine))
                {
                    endAddressLine = endPlace.FormattedAddress.TakeWhile(c => c != ',').ToString();
                }
                if (string.IsNullOrEmpty(endAddressLine))
                    throw new ArgumentException("Cannot calculate end address line", nameof(endPlace));
                requestBody["endAddressLine"] = endAddressLine;

                if (endDateTime != null)
                {
                    requestBody.Add("endDateTime", $"{endDateTime:s}");
                }
                if (transferType != null)
                {
                    requestBody.Add("transferType", transferType.ToString());
                }
                if (vehicleCategory != null)
                {
                    requestBody.Add("vehicleCategory", vehicleCategory.ToString());
                }
                if (vehicleType != null)
                {
                    requestBody.Add("vehicleCode", vehicleType.ToString());
                }

                var passengerCharacteristics = new List<Dictionary<string, object>>();
                for (int i = 0; i < passengers - (children ?? 0); i++)
                {
                    passengerCharacteristics.Add(new Dictionary<string, object> { { "passengerTypeCode", "ADT" }, { "age", 20 } });
                }
                for (int i = 0; i < (children ?? 0); i++)
                {
                    passengerCharacteristics.Add(new Dictionary<string, object> { { "passengerTypeCode", "CHD" }, { "age", 10 } });
                }
                requestBody.Add("passengerCharacteristics", passengerCharacteristics.ToArray());

                var jsonRequestBody = JsonConvert.SerializeObject(requestBody);

                List<TransferOffer> offers = new();
                using HttpClient client = new();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.amadeus+json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                HttpResponseMessage transferResponse = await client.PostAsJsonAsync("https://test.api.amadeus.com/v1/shopping/transfer-offers", requestBody);

                if (transferResponse.IsSuccessStatusCode)
                {
                    string responseContent = await transferResponse.Content.ReadAsStringAsync();
                    JObject transfersData = JObject.Parse(responseContent);

                    if (!transfersData.ContainsKey("data"))
                    {
                        if (transfersData.ContainsKey("errors") && int.Parse(transfersData["errors"][0]["code"].ToString()) == 38192)
                        {
                            SetClient();
                            return await GetTransferOffers(startPlace, endPlace, startDateTime, passengers, children, endDateTime,
                                transferType, vehicleCategory, vehicleType, lang);
                        }
                        else
                            throw new Exception("An error occurred, please try again later");
                    }

                    foreach (var offerData in transfersData["data"])
                    {
                        var offerId = offerData["id"]?.ToString() ?? null!;

                        TransferType tranType = (TransferType)Enum.Parse(typeof(TransferType), offerData["transferType"]?.ToString() ?? string.Empty);
                        VehicleType vehType = (VehicleType)Enum.Parse(typeof(VehicleType), offerData["vehicle"]?["code"]?.ToString() ?? string.Empty);
                        VehicleCategory vehCategory = (VehicleCategory)Enum.Parse(typeof(VehicleCategory), offerData["vehicle"]?["category"]?.ToString() ?? string.Empty);

                        var description = offerData["vehicle"]?["description"]?.ToString();

                        var iconUrl = offerData["vehicle"]?["imageURL"]?.ToString();
                        var providerName = offerData["serviceProvider"]?["name"]?.ToString();
                        var providerLogoUrl = offerData["serviceProvider"]?["logoUrl"]?.ToString();

                        DateTime startDate = DateTime.Parse(offerData["start"]?["dateTime"]?.ToString() ?? string.Empty);
                        DateTime endDate = DateTime.Parse(offerData["end"]?["dateTime"]?.ToString() ?? string.Empty);

                        int? distance = Convert.ToInt32(offerData["distance"]?["value"]?.ToString() ?? "-1");
                        distance = distance == -1 ? null : distance;
                        var distanceUnit = offerData["distance"]?["unit"]?.ToString();

                        double price = Convert.ToDouble(offerData["quotation"]?["monetaryAmount"]?.ToString() ?? string.Empty, CultureInfo.InvariantCulture);
                        var currency = offerData["quotation"]?["currencyCode"]?.ToString();
                        List<PaymentMethod> paymentMethods = new();
                        var methodsInput = offerData["methodsOfPaymentAccepted"]?.ToString();
                        if (methodsInput != null)
                        {
                            List<string> methodslist = JsonConvert.DeserializeObject<List<string>>(methodsInput);
                            foreach (var method in methodslist)
                            {
                                try
                                {
                                    paymentMethods.Add((PaymentMethod)Enum.Parse(typeof(PaymentMethod), method));
                                }
                                catch
                                {
                                    // Skip unacceptable methods
                                }
                            }
                        }
                        var cancellationRules = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(offerData["cancellationRules"]?.ToString() ?? string.Empty);

                        offers.Add(new()
                        {
                            ActivityId = offerId,
                            StartDate = startDate,
                            StartLocation = startPlace,
                            EndDate = endDate,
                            EndLocation = endPlace,
                            TranType = tranType,
                            VehType = vehType,
                            VehCategory = vehCategory,
                            Description = description,
                            CarIconURL = iconUrl,
                            ProviderName = providerName,
                            ProviderLogoUrl = providerLogoUrl,
                            DistanceValue = distance,
                            DistanceUnit = distanceUnit,
                            PriceAmount = price,
                            Currency = currency,
                            PaymentMethods = paymentMethods,
                            CancellationRules = cancellationRules ?? new()
                        });
                    }
                }
                else
                    throw new Exception("Bad response from \"shopping/transfer-offers\"");

                return offers;
            }
            catch (Exception ex)
            {
                string mess = ex.ToString();
                throw;
            }
        }
        
        public async Task<TransferOrder> GetTransferOrders(TransferOffer transferOffer, List<User> passengers, Models.CreditCard credit, string agencyName, int cvv)
        {
            if (cvv < 100 || cvv >= 1000)
                throw new ArgumentException("Invalid CVV", nameof(cvv));
            if (passengers.Count < 1)
                throw new ArgumentException("Check the passenger list for completeness", nameof(passengers));
            if (string.IsNullOrEmpty(credit.Number))
                throw new ArgumentException("Check the card details for completeness", nameof(passengers));

            List<Dictionary<string, object>> passengersPart = new();
            foreach (var pass in passengers)
            {
                passengersPart.Add(new()
                {
                    ["firstName"] = pass.FirstName,
                    ["lastName"] = pass.LastName,
                    ["title"] = pass.Gender == Gender.FL ? "MS" : "MR",
                    ["contacts"] = new Dictionary<string, object>
                    {
                        ["phoneNumber"] = pass.Phone,
                        ["email"] = pass.Email
                    }
                });
            }

            var requestBody = new Dictionary<string, object>
            {
                ["data"] = new Dictionary<string, object>
                {
                    ["passengers"] = passengersPart,
                    ["payment"] = new Dictionary<string, object>
                    {
                        ["methodOfPayment"] = "CREDIT_CARD",
                        ["creditCard"] = new Dictionary<string, object>
                        {
                            ["number"] = credit.Number.Replace(" ", ""),
                            ["holderName"] = agencyName.ToUpper(),
                            ["vendorCode"] = credit.VendorCode.ToString(),
                            ["expiryDate"] = credit.ExpiryDate.ToString("yyyy-MM"),
                            ["cvv"] = cvv.ToString(),
                        }
                    }
                }
            };
            string jsonRequestBody = JsonConvert.SerializeObject(requestBody);

            try
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.amadeus+json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                HttpResponseMessage ordersResponse =
                await client.PostAsJsonAsync($"https://test.api.amadeus.com/v1/ordering/transfer-orders?offerId={transferOffer.ActivityId}", requestBody);


                if (ordersResponse.IsSuccessStatusCode)
                {
                    string responseContent = await ordersResponse.Content.ReadAsStringAsync();
                    JObject transfersData = JObject.Parse(responseContent);

                    if (!transfersData.ContainsKey("data"))
                    {
                        if (transfersData.ContainsKey("errors") && int.Parse(transfersData["errors"][0]["code"].ToString()) == 38192)
                        {
                            SetClient();
                            return await GetTransferOrders(transferOffer, passengers, credit, agencyName, cvv);
                        }
                        else
                            throw new Exception("An error occurred, please try again later");
                    }


                    string orderId = transfersData["data"]?["id"]?.ToString() ?? string.Empty;
                    string reference = transfersData["data"]?["reference"]?.ToString() ?? string.Empty;

                    TransferOrder order = new()
                    {
                        OrderId = orderId,
                        Reference = reference,
                        TransferOffer = transferOffer
                    };

                    return order;
                }
                else
                {
                    string response = string.Empty;
                    string responseContent = await ordersResponse.Content.ReadAsStringAsync();
                    JObject transfersData = JObject.Parse(responseContent);
                    if (Convert.ToInt32(transfersData["code"].ToString()) == 33553)
                        response = "Order ID has expired, please update your orders and try again";

                    throw new Exception(!string.IsNullOrEmpty(response) ? response : $"Bad response from \"booking/hotel-bookings\". {responseContent}");
                }
            }
            catch (Exception ex)
            {
                string mess = ex.Message;
                throw;
            }
        }

        public async Task CancelTransferOrders(string orderId, string confirmNbr)
        {
            try
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.amadeus+json"));
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {bearerToken}");
                HttpResponseMessage ordersResponse =
                await client.PostAsJsonAsync($"https://test.api.amadeus.com/v1/ordering/transfer-orders/{orderId}/transfers/cancellation?confirmNbr={confirmNbr}", "");

                if (ordersResponse.IsSuccessStatusCode)
                {
                    string responseContent = await ordersResponse.Content.ReadAsStringAsync();
                    JObject transfersData = JObject.Parse(responseContent);

                    if (!transfersData.ContainsKey("data"))
                    {
                        if (transfersData.ContainsKey("errors") && int.Parse(transfersData["errors"][0]["code"].ToString()) == 38192)
                        {
                            SetClient();
                            await CancelTransferOrders(orderId, confirmNbr);
                        }
                        else
                            throw new Exception("An error occurred, please try again later");
                    }
                }
            }
            catch (Exception ex)
            {
                string mess = ex.Message;
                throw;
            }
        }

        #endregion

        #region PoI
        public async Task<List<PointOfInteres>> GetPointsOfInterestAsync(double latitude, double longitude, int radius = 1,
            LocationCategory[]? categories = null)
        {
            if (radius < 1 || radius > 20)
                throw new ArgumentOutOfRangeException(nameof(radius), "The radius value must be between 1 and 20 inclusive");

            List<PointOfInteres> PoIs = new();

            var culture = new CultureInfo("en-US");
            Params @params = Params.
                with("latitude", $"{latitude.ToString("G", culture)}").
                and("longitude", $"{longitude.ToString("G", culture)}").
                and("radius", $"{radius}");

            if (categories != null)
            {
                @params.and("categories", $"{string.Join(',', categories)}");
            }
            string queryString = string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"));
            string requestUrl = $"v1/reference-data/locations/pois?{queryString}";

            HttpResponseMessage poisResponse = await amadeusClient.GetAsync(requestUrl);
            string poisStr = await poisResponse.Content.ReadAsStringAsync();

            if (poisResponse != null && poisStr.Length > 0)
            {
                JObject poisData = JObject.Parse(poisStr);

                if (!poisData.ContainsKey("data"))
                {
                    if (poisData.ContainsKey("errors") && int.Parse(poisData["errors"][0]["code"].ToString()) == 38192)
                    {
                        SetClient();
                        return await GetPointsOfInterestAsync(latitude, longitude, radius, categories);
                    }
                    else
                        throw new Exception("An error occurred, please try again later");
                }

                foreach (var poiData in poisData["data"])
                {
                    string poiId = poiData["id"]?.ToString() ?? string.Empty;
                    string? poiName = poiData["name"]?.ToString();

                    var geo = JsonConvert.DeserializeObject<Dictionary<string, double>>(poiData["geoCode"]?.ToString() ?? string.Empty);
                    double? poiLatitude = geo?["latitude"];
                    double? poiLongitude = geo?["longitude"];

                    LocationCategory poiCategory = (LocationCategory)Enum.Parse(typeof(LocationCategory), poiData["category"]?.ToString() ?? string.Empty);
                    int? poiRank = int.Parse(poiData["rank"]?.ToString() ?? "-1");
                    poiRank = poiRank == -1 ? null : poiRank;
                    var poiTags = JsonConvert.DeserializeObject<List<string>>(poiData["tags"]?.ToString() ?? string.Empty);

                    PointOfInteres poi = new()
                    {
                        ActivityId = poiId,
                        Name = poiName,
                        Category = poiCategory,
                        Rank = poiRank,
                        Tags = poiTags ?? new()
                    };
                    if (poiLatitude != null && poiLongitude != null)
                    {
                        poi.Location = new()
                        {
                            Latitude = poiLatitude.Value,
                            Longitude = poiLongitude.Value
                        };
                    }

                    PoIs.Add(poi);
                }
            }
            else
                throw new Exception("Bad response from \"locations/pois\"");

            return PoIs;
        }
        #endregion

        #region Tours and Side activities
        public async Task<List<TourOrSideActivity>> GetSidesAsync(double latitude, double longitude, int radius = 1)
        {
            if (radius < 1 || radius > 20)
                throw new ArgumentOutOfRangeException(nameof(radius), "The radius must be within 1-20 inclusive");

            List<TourOrSideActivity> activities = new();

            var culture = new CultureInfo("en-US");
            Params @params = Params.
                with("latitude", $"{latitude.ToString("G", culture)}").
                and("longitude", $"{longitude.ToString("G", culture)}").
                and("radius", $"{radius}");

            string queryString = string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"));
            string requestUrl = $"v1/shopping/activities?{queryString}";

            HttpResponseMessage activitiesResponse = await amadeusClient.GetAsync(requestUrl);
            string activitiesStr = await activitiesResponse.Content.ReadAsStringAsync();


            if (activitiesResponse != null && activitiesStr.Length > 0)
            {
                JObject activitiesData = JObject.Parse(activitiesStr);

                if (!activitiesData.ContainsKey("data"))
                {
                    if (activitiesData.ContainsKey("errors") && int.Parse(activitiesData["errors"][0]["code"].ToString()) == 38192)
                    {
                        SetClient();
                        return await GetSidesAsync(latitude, longitude, radius);
                    }
                    else
                        throw new Exception("An error occurred, please try again later");
                }

                foreach (var activityData in activitiesData["data"])
                {
                    string? actName = activityData["name"]?.ToString();

                    string? actShortDesc = activityData["shortDescription"]?.ToString();
                    string? actDesc = activityData["description"]?.ToString();

                    string? actBookingLink = activityData["bookingLink"]?.ToString();

                    if (string.IsNullOrEmpty(actBookingLink)
                        || string.IsNullOrEmpty(actName)
                        || string.IsNullOrEmpty(actShortDesc) && string.IsNullOrEmpty(actDesc))
                        continue;

                    string? actId = activityData["id"]?.ToString();

                    double? actRating = double.Parse(activityData["rating"]?.ToString() ?? "-1", CultureInfo.InvariantCulture);
                    actRating = actRating == -1 ? null : actRating;

                    var geo = JsonConvert.DeserializeObject<Dictionary<string, double>>(activityData["geoCode"]?.ToString() ?? string.Empty);
                    double? actLatitude = geo?["latitude"];
                    double? actLongitude = geo?["longitude"];

                    List<string>? actPictures = JsonConvert.DeserializeObject<List<string>>(activityData["pictures"]?.ToString() ?? string.Empty);

                    double? actPrice = double.Parse(activityData["price"]?["amount"]?.ToString() ?? "-1", CultureInfo.InvariantCulture);
                    actPrice = actPrice == -1 ? null : actPrice;
                    string? actCurrency = activityData["price"]?["currencyCode"]?.ToString();

                    string? actMinimumDuration = activityData["minimumDuration"]?.ToString();

                    TourOrSideActivity tos = new()
                    {
                        ActivityId = actId ?? string.Empty,
                        Title = actName,
                        ShortDescription = actShortDesc,
                        Description = actDesc,
                        Rating = actRating,
                        PicturesUrls = actPictures ?? new(),
                        BookingLink = actBookingLink,
                        PriceAmount = actPrice,
                        Currency = actCurrency,
                        MinimumDuration = actMinimumDuration
                    };
                    if (actLatitude != null && actLongitude != null)
                    {
                        tos.Location = new()
                        {
                            Latitude = actLatitude.Value,
                            Longitude = actLongitude.Value
                        };
                    }

                    activities.Add(tos);
                }
            }
            else
                throw new Exception("Bad response from \"shopping/Activities\"");

            return activities;
        }
        #endregion

        #region City
        public async Task<List<City>> GetCitiesByCountryCode(string countryCode, string cityName)
        {
            if (string.IsNullOrEmpty(countryCode) || countryCode.Any(c => !char.IsAsciiLetter(c)))
                throw new ArgumentException("Invalid country code format", nameof(countryCode));
            if (string.IsNullOrEmpty(cityName) || cityName.Any(c => !char.IsAsciiLetter(c)))
                throw new ArgumentException("Invalid city name format", nameof(cityName));

            List<City> cities = new();
            Params @params = Params.
                with("countryCode", countryCode).
                and("keyword", cityName);

            string queryString = string.Join("&", @params.Select(x => $"{x.Key}={x.Value}"));
            string requestUrl = $"v1/reference-data/locations/cities?{queryString}";

            HttpResponseMessage citiesResponse = await amadeusClient.GetAsync(requestUrl);
            string citiesStr = await citiesResponse.Content.ReadAsStringAsync();

            if (citiesResponse != null && citiesStr.Length > 0)
            {
                JObject citiesData = JObject.Parse(citiesStr);

                if (!citiesData.ContainsKey("data"))
                {
                    if (citiesData.ContainsKey("errors") && int.Parse(citiesData["errors"][0]["code"].ToString()) == 38192)
                    {
                        SetClient();
                        return await GetCitiesByCountryCode(countryCode, cityName);
                    }
                    else
                        throw new Exception("An error occurred, please try again later");
                }

                foreach (var сityData in citiesData["data"])
                {
                    string? ctName = сityData["name"]?.ToString();
                    string? ctIata = сityData["iataCode"]?.ToString();

                    string? cCode = сityData["address"]?["countryCode"]?.ToString();
                    if (cCode == null || cCode != countryCode)
                        continue;

                    string? geoCodeStr = сityData["geoCode"]?.ToString();
                    if (geoCodeStr == null)
                        continue;
                    Dictionary<string, double> geoCode = JsonConvert.DeserializeObject<Dictionary<string, double>>(geoCodeStr);

                    City city = new()
                    {
                        Name = ctName,
                        IataCode = ctIata,
                        GeoCode = geoCode
                    };
                    cities.Add(city);
                }
            }
            else
                throw new Exception("Bad response from \"locations/cities\"");

            return cities;
        }
        #endregion
    }
}
