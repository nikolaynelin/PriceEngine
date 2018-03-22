namespace STN.PriceEngine.Context
{
    using System.Data.Entity;

    public partial class StnContext : DbContext
    {
        public StnContext()
            : base("name=StnContext")
        {
        }

        public virtual DbSet<PeAuto_RuleTicketEvents4> PeAuto_RuleTicketEvents4 { get; set; }
        public virtual DbSet<ProcessedSuggestion> ProcessedSuggestions { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PeAuto_RuleTicketEvents4>()
                .Property(e => e.TrtPrice)
                .HasPrecision(19, 4);

            modelBuilder.Entity<PeAuto_RuleTicketEvents4>()
                .Property(e => e.TrtSugPrice)
                .HasPrecision(19, 4);
        }
    }
}
