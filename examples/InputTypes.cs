using System;
using System.Collections.Generic;

namespace JsonConverterGenerator
{
    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }

    public class Location
    {
        public int Id { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
    }

    public class IndexViewModel 
    {
        public List<ActiveOrUpcomingEvent> ActiveOrUpcomingEvents { get; set; }
        public CampaignSummaryViewModel FeaturedCampaign { get; set; }
        public bool IsNewAccount { get; set; }
        public bool HasFeaturedCampaign => FeaturedCampaign != null;
    }

    public class ActiveOrUpcomingEvent
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public string Name { get; set; }
        public string CampaignName { get; set; }
        public string CampaignManagedOrganizerName { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
    }

    public class CampaignSummaryViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string OrganizationName { get; set; }
        public string Headline { get; set; }
    }

    public class MyEventsListerViewModel
    {
        public List<MyEventsListerItem> CurrentEvents { get; set; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> FutureEvents { get; set; } = new List<MyEventsListerItem>();
        public List<MyEventsListerItem> PastEvents { get; set; } = new List<MyEventsListerItem>();

    }

    public class MyEventsListerItem
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public string TimeZone { get; set; }
        public string Campaign { get; set; }
        public string Organization { get; set; }
        public int VolunteerCount { get; set; }

        public List<MyEventsListerItemTask> Tasks { get; set; } = new List<MyEventsListerItemTask>();
    }

    public class MyEventsListerItemTask
    {
        public string Name { get; set; }
        // TODO: revert this to Nullable type.
        public DateTimeOffset StartDate { get; set; }
        // TODO: revert this to Nullable type.
        public DateTimeOffset EndDate { get; set; }

        public string FormattedDate
        {
            get
            {
                var startDateString = string.Format("{0:g}", StartDate);
                var endDateString = string.Format("{0:g}", EndDate);

                return string.Format($"From {startDateString} to {endDateString}");
            }
        }
    }

    public class CollectionsOfPrimitives
    {
        public byte[] ByteArray { get; set; }
        public DateTime[] DateTimeArray { get; set; }
        // TODO: revert this to string key.
        public Dictionary<string, string> Dictionary { get; set; }
        public List<int> ListOfInt { get; set; }
    }
}
