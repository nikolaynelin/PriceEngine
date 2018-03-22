namespace STN.PriceEngine.Context
{
    using System;
    using System.ComponentModel.DataAnnotations;

    public partial class ProcessedSuggestion
    {
        public int local_ticket_id { get; set; }

        public DateTime RunDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; }

        public int Price { get; set; }

        public bool IsProcessed { get; set; }

        public bool RunRule { get; set; }

        public int Id { get; set; }
    }
}
