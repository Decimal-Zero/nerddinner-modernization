using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using NetTopologySuite.Geometries;

namespace NerdDinner.Proxy.Models
{
    // Ported from the legacy app's Models/Dinner.cs (M9, decision-log.md
    // DL-028). Same validation rules and business logic (IsHostedBy,
    // IsUserRegistered) -- only the spatial type changed, per plan.md's M9
    // acceptance criteria: System.Data.Entity.Spatial.DbGeography (EF6)
    // becomes NetTopologySuite.Geometries.Point (EF Core), mapped to the
    // same "geography" SQL Server column the legacy app's EF6 Migrations
    // created (src/Migrations/*_InitialCreate.cs) -- this app reads/writes
    // the same physical "Dinners"/"RSVPs" tables, not a separate schema.
    public class Dinner
    {
        public int DinnerID { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(50, ErrorMessage = "Title may not be longer than 50 characters")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Event Date is required")]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(256, ErrorMessage = "Description may not be longer than 256 characters")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        [StringLength(20, ErrorMessage = "Hosted By name may not be longer than 20 characters")]
        [Display(Name = "Host's Name")]
        public string HostedBy { get; set; }

        [Required(ErrorMessage = "Contact info is required")]
        [StringLength(20, ErrorMessage = "Contact info may not be longer than 20 characters")]
        [Display(Name = "Contact Info")]
        public string ContactPhone { get; set; }

        [Required(ErrorMessage = "Address is required")]
        [StringLength(50, ErrorMessage = "Address may not be longer than 50 characters")]
        [Display(Name = "Address, City, State, ZIP")]
        public string Address { get; set; }

        [UIHint("CountryDropDown")]
        public string Country { get; set; }

        // Nullable -- matches the legacy schema's "geography" column,
        // which EF6's Code First Migrations left nullable (no
        // nullable:false), and the model's own contract (no [Required]
        // attribute; SearchController's JsonDinnerFromDinner NRE-on-null
        // characterized bug depends on a null Location being legal). With
        // <Nullable>enable</Nullable> on this project, a non-nullable
        // Point here would make EF Core treat the column as required by
        // convention -- found by an actual failed insert during testing,
        // not assumed.
        [Display(Name = "Location")]
        public Point? Location { get; set; }

        // Deliberately NOT default-initialized -- IsUserRegistered throwing
        // on a freshly-constructed Dinner with no RSVPs loaded yet is a
        // characterized behavior (NerdDinner.Tests DinnerTests
        // .IsUserRegistered_ThrowsNullReferenceException_WhenRSVPsIsNull).
        //
        // [BindNever]: RSVPs is never posted from a Create/Edit form (it's
        // set server-side in DinnersController.Create). [ValidateNever]:
        // required in addition -- [BindNever] alone stops MVC from
        // *populating* this property from posted data, but the property
        // then stays null, and ASP.NET Core MVC's validation step
        // separately flags any non-nullable reference-type property as
        // implicitly required under <Nullable>enable</Nullable>
        // regardless of whether binding was attempted, rejecting every
        // Create POST with "The RSVPs field is required." even after
        // adding [BindNever] alone -- confirmed by an actual failed Create
        // submission both before and after that first fix (M10,
        // decision-log.md DL-029), the same class of implicit-nullability
        // surprise DL-028 hit for Point/Location in M9 -- there it was a
        // required database column, here it's an unwanted required form
        // field.
        [BindNever]
        [ValidateNever]
        public virtual ICollection<RSVP> RSVPs { get; set; }

        public bool IsHostedBy(string userName)
        {
            return String.Equals(HostedBy, userName, StringComparison.Ordinal);
        }

        public bool IsUserRegistered(string userName)
        {
            return RSVPs.Any(r => r.AttendeeName == userName);
        }

        [UIHint("LocationDetail")]
        [NotMapped]
        public LocationDetail LocationDetail
        {
            get
            {
                return new LocationDetail() { Location = this.Location, Id = this.DinnerID, Title = this.Title, Address = this.Address };
            }
            set
            {
                this.Location = value.Location;
                this.DinnerID = value.Id;
                this.Title = value.Title;
                this.Address = value.Address;
            }
        }
    }

    public class LocationDetail
    {
        public Point? Location;
        public int Id;
        public string Title;
        public string Address;
    }
}
