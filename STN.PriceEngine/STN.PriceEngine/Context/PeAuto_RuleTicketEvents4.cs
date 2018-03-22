namespace STN.PriceEngine.Context
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public partial class PeAuto_RuleTicketEvents4
    {
        public int? local_ticket_id { get; set; }

        public int? Event_ID { get; set; }

        [StringLength(50)]
        public string Action { get; set; }

        public int? Price { get; set; }

        public int? SugPrice { get; set; }

        [Column(TypeName = "money")]
        public decimal? TrtPrice { get; set; }

        [Column(TypeName = "money")]
        public decimal? TrtSugPrice { get; set; }

        public DateTime? RunDate { get; set; }

        public DateTime? Event_Date { get; set; }

        public DateTime Event_Time { get; set; }

        [StringLength(255)]
        public string Venue { get; set; }

        [StringLength(255)]
        public string Event { get; set; }

        public int Id { get; set; }
    }
}
