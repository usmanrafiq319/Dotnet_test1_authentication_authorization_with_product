using Dotnet_test1_authentication_authorization_with_product.Entities;
using Microsoft.EntityFrameworkCore;
namespace Dotnet_test1_authentication_authorization_with_product.Data
{
    public class UserDbContext( DbContextOptions<UserDbContext> options):DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Otp> Otps { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithOne(u => u.Cart)
                .HasForeignKey<Cart>(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Profile>()
                .HasOne(p => p.User)
                .WithOne(u => u.Profile)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Otp>()
                .HasOne(o => o.User)
                .WithMany(u => u.Otps)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Otp>()
                .Property(e => e.Purpose)
                .HasConversion<string>();

            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();


            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(message => message.Id);

                entity.Property(message => message.Text)
                    .HasMaxLength(2000)
                    .IsRequired();

                entity.Property(message => message.SenderType)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasOne(message => message.Conversation)
                    .WithMany(conversation => conversation.Messages)
                    .HasForeignKey(message => message.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(message => new
                {
                    message.ConversationId,
                    message.SentAt
                });
            });

            modelBuilder.Entity<Conversation>(entity =>
            {
                entity.HasKey(conversation =>
                    conversation.Id
                );

                entity.HasIndex(conversation =>
                    conversation.UserId
                )
                .IsUnique();

                entity.Property(conversation =>
                    conversation.Mode
                )
                .HasConversion<string>()
                .HasMaxLength(20);

                entity.HasOne(conversation =>
                    conversation.User
                )
                .WithOne(user =>
                    user.Conversation
                )
                .HasForeignKey<Conversation>(
                    conversation =>
                        conversation.UserId
                )
                .OnDelete(DeleteBehavior.Cascade);
            });


            //modelBuilder.Entity<Conversation>(entity =>
            //{
            //    entity.HasKey(x => x.Id);

            //    // Each normal user can have only one conversation
            //    entity.HasIndex(x => x.UserId)
            //        .IsUnique();

            //    entity.HasOne(x => x.User)
            //        .WithOne(x => x.Conversation)
            //        .HasForeignKey<Conversation>(x => x.UserId)
            //        .OnDelete(DeleteBehavior.Cascade);
            //});

            //modelBuilder.Entity<ChatMessage>(entity =>
            //{
            //    entity.HasKey(x => x.Id);

            //    entity.Property(x => x.Text)
            //        .IsRequired()
            //        .HasMaxLength(2000);

            //    entity.HasIndex(x => new
            //    {
            //        x.ConversationId,
            //        x.SentAt
            //    });

            //    entity.HasOne(x => x.Conversation)
            //        .WithMany(x => x.Messages)
            //        .HasForeignKey(x => x.ConversationId)
            //        .OnDelete(DeleteBehavior.Cascade);
            //});


            //modelBuilder.Entity<PasswordResetOtp>()
            //    .HasOne(p => p.User)
            //    .WithOne(u => u.PasswordResetOtp)
            //    .HasForeignKey<PasswordResetOtp>(p => p.UserId)
            //    .OnDelete(DeleteBehavior.Cascade);

            //modelBuilder.Entity<Profile>()
            //    .HasIndex(p => p.Email)
            //    .IsUnique();


        }

    }
}
