using Innofactor.EfCoreJsonValueConverter;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Models;

namespace Travel_App_Web.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<TourAgency> Agencies { get; set; }
        public DbSet<Tour> Tours { get; set; }
        public DbSet<ParticipantUnit> ParticipantUnits { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Country> Countries { get; set; }
        public DbSet<City> Cities { get; set; }
        public DbSet<Hotel> Hotels { get; set; }
        public DbSet<PolicyDetails> PolicyDetails { get; set; }
        public DbSet<Chat> Chats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Role userRole = new() { RoleId = 1, RoleName = "User" };
            Role adminRole = new() { RoleId = 2, RoleName = "Admin" };
            Role managerRole = new() { RoleId = 3, RoleName = "Manager" };

            modelBuilder.Entity<Role>().HasData(new Role[] { userRole, adminRole, managerRole });

            modelBuilder.AddJsonFields();


            #region User Settings

            modelBuilder.Entity<User>()
            .Property(user => user.Gender)
            .HasConversion(
                    v => v.ToString(),
                    v => (Gender)Enum.Parse(typeof(Gender), v),
                    new ValueComparer<Gender>(
                        (g1, g2) => g1 == g2,
                        g => g.GetHashCode(),
                        g => g));

            modelBuilder.Entity<User>()
                .HasOne(user => user.Country)
                .WithMany();

            modelBuilder.Entity<User>()
                .HasOne(user => user.Role)
                .WithMany();

            modelBuilder.Entity<User>()
                .HasMany(user => user.Chats)
                .WithMany();

            #endregion

            #region Chat Settings

            modelBuilder.Entity<Chat>()
                .OwnsMany(chat => chat.Messages);

            #endregion

            #region Tour Settings

            modelBuilder.Entity<Tour>()
                .HasOne(tour => tour.Agency)
                .WithMany();

            modelBuilder.Entity<Tour>()
                .HasMany(tour => tour.Participants)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tour>()
                .HasMany(tour => tour.Cities)
                .WithMany();

            modelBuilder.Entity<Tour>()
                .HasMany(tour => tour.Program)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tour>()
                .HasMany(tour => tour.Hotels)
                .WithMany();

            modelBuilder.Entity<Tour>()
                .HasMany(tour => tour.HotelsOffers)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Tour>()
                .HasMany(tour => tour.TransferOrders)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region Hotel Settings

            modelBuilder.Entity<Hotel>()
                .HasOne(hotel => hotel.City)
                .WithMany();

            modelBuilder.Entity<Hotel>()
                .Property(hotel => hotel.Sentiments)
                .HasJsonValueConversion();

            modelBuilder.Entity<HotelsOffer>()
                .Property(offer => offer.Boards)
                .HasJsonValueConversion();

            modelBuilder.Entity<HotelsOffer>()
                .HasOne(offer => offer.Room)
                .WithOne()
                .HasForeignKey<Room>(room => room.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HotelsOffer>()
                .HasOne(offer => offer.PolicyDetails)
                .WithOne()
                .HasForeignKey<PolicyDetails>(pd => pd.OfferId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyDetails>()
                .HasOne(policy => policy.Guarantee)
                .WithOne()
                .HasForeignKey<Policy>(guarantee => guarantee.PolicyDetailsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyDetails>()
                .HasOne(policy => policy.Deposit)
                .WithOne()
                .HasForeignKey<Policy>(guarantee => guarantee.PolicyDetailsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyDetails>()
                .HasOne(policy => policy.Prepay)
                .WithOne()
                .HasForeignKey<Policy>(guarantee => guarantee.PolicyDetailsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyDetails>()
                .HasOne(policy => policy.HoldTime)
                .WithOne()
                .HasForeignKey<Policy>(guarantee => guarantee.PolicyDetailsId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PolicyDetails>()
                .Property(policy => policy.Cancellations)
                .HasJsonValueConversion();

            modelBuilder.Entity<Policy>()
                .Property(guarantee => guarantee.CreditCards)
                .HasJsonValueConversion();

            modelBuilder.Entity<Policy>()
                .Property(guarantee => guarantee.Methods)
                .HasJsonValueConversion();

            modelBuilder.Entity<HotelOrder>()
                .HasOne(order => order.HotelsOffer)
                .WithOne()
                .HasForeignKey<HotelsOffer>(ho => ho.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            #endregion

            #region Google Place Settings

            modelBuilder.Entity<GooglePlace>()
                .HasOne(place => place.Location)
                .WithOne()
                .HasForeignKey<Location>(loc => loc.PlaceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GooglePlace>()
                .Property(place => place.WeekdayDescriptions)
                .HasJsonValueConversion();

            modelBuilder.Entity<Location>()
                .Property(location => location.AddressComponents)
                .HasJsonValueConversion();

            modelBuilder.Entity<Location>()
                .Property(location => location.ImagesPaths)
                .HasJsonValueConversion();

            #endregion

            #region Activity Settings

            modelBuilder.Entity<Activity>()
                .HasKey(a => new { a.ActivityId, a.TourId });

            modelBuilder.Entity<Activity>()
                .HasOne<Tour>()
                .WithMany(tour => tour.Program)
                .HasForeignKey(a => a.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransferOrder>()
                .HasOne(to => to.TransferOffer)
                .WithOne()
                .HasForeignKey<TransferOffer>(to => to.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransferOffer>()
                .HasOne(to => to.StartLocation)
                .WithMany()
                .HasForeignKey(to => to.StartLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransferOffer>()
                .HasOne(to => to.EndLocation)
                .WithMany()
                .HasForeignKey(to => to.EndLocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransferOffer>()
                .Property(to => to.CancellationRules)
                .HasJsonValueConversion();

            modelBuilder.Entity<TransferOffer>()
                .Property(to => to.PaymentMethods)
                .HasJsonValueConversion();

            modelBuilder.Entity<PointOfInteres>()
                .Property(poi => poi.Tags)
                .HasJsonValueConversion();

            modelBuilder.Entity<PointOfInteres>()
                .HasOne(poi => poi.Location)
                .WithOne()
                .HasForeignKey<PointOfInteres>(poi => poi.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TourOrSideActivity>()
                .Property(tos => tos.PicturesUrls)
                .HasJsonValueConversion();

            modelBuilder.Entity<TourOrSideActivity>()
                .HasOne(poi => poi.Location)
                .WithOne()
                .HasForeignKey<TourOrSideActivity>(poi => poi.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region TourAgency Settings

            modelBuilder.Entity<TourAgency>()
                .HasOne(agency => agency.Country)
                .WithMany();

            modelBuilder.Entity<TourAgency>()
                .Property(agency => agency.PhoneNumbers)
                .HasJsonValueConversion();

            modelBuilder.Entity<TourAgency>()
                .HasMany(agency => agency.Managers)
                .WithMany();

            modelBuilder.Entity<TourAgency>()
                .OwnsOne(agency => agency.CreditCard);

            #endregion

            #region City Settings

            modelBuilder.Entity<City>()
                .HasOne(city => city.Country)
                .WithMany();

            modelBuilder.Entity<City>()
                .Property(city => city.GeoCode)
                .HasJsonValueConversion();

            #endregion

            #region Country Settings

            modelBuilder.Entity<Country>()
                .Property(country => country.Languages)
                .HasJsonValueConversion();

            modelBuilder.Entity<Country>()
                .Property(country => country.Currencies)
                .HasJsonValueConversion();

            modelBuilder.Entity<Country>()
                .Property(country => country.Timezones)
                .HasJsonValueConversion();

            modelBuilder.Entity<Country>()
                .HasOne(country => country.IDD)
                .WithOne()
                .HasForeignKey<Idd>(idd => idd.CountryCCA2)
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region Idd Settings

            modelBuilder.Entity<Idd>()
                .Property(idd => idd.Suffixes)
                .HasJsonValueConversion();

            #endregion

            #region ParticipantUnit Settings

            modelBuilder.Entity<ParticipantUnit>()
                .HasMany(unit => unit.OtherUsers)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ParticipantUnit>()
                .HasOne(unit => unit.PrimaryUser)
                .WithMany();

            modelBuilder.Entity<ParticipantUnit>()
                .OwnsOne(unit => unit.CreditCard);

            modelBuilder.Entity<ParticipantUnit>()
                .HasMany(part => part.HotelsOrders)
                .WithOne()
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region Participant Settings

            modelBuilder.Entity<Participant>()
                .HasOne(part => part.User)
                .WithMany()
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Participant>()
                .Property(part => part.Gender)
                .HasConversion(
                    v => v.ToString(),
                    v => (Gender)Enum.Parse(typeof(Gender), v),
                        new ValueComparer<Gender>(
                            (g1, g2) => g1 == g2,
                            g => g.GetHashCode(),
                            g => g));

            #endregion

            base.OnModelCreating(modelBuilder);
        }
    }
}
