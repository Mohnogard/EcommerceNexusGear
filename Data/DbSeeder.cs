using Microsoft.AspNetCore.Identity;
using NexusGear.Models;

namespace NexusGear.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            await context.Database.EnsureCreatedAsync();

            foreach (var role in new[] { "Admin", "Customer" })
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            if (await userManager.FindByEmailAsync("admin@nexusgear.com") == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "admin@nexusgear.com",
                    Email = "admin@nexusgear.com",
                    FirstName = "Admin",
                    LastName = "NexusGear",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded) await userManager.AddToRoleAsync(admin, "Admin");
            }

            if (await userManager.FindByEmailAsync("gamer@nexusgear.com") == null)
            {
                var customer = new ApplicationUser
                {
                    UserName = "gamer@nexusgear.com",
                    Email = "gamer@nexusgear.com",
                    FirstName = "Alex",
                    LastName = "Gamer",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(customer, "Customer@123");
                if (result.Succeeded) await userManager.AddToRoleAsync(customer, "Customer");
            }

            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new() { Name = "Gaming Mice", Description = "High-precision gaming mice for competitive play", Slug = "gaming-mice", ImageUrl = "/images/categories/mice.jpg", DisplayOrder = 1 },
                    new() { Name = "Keyboards", Description = "Mechanical and membrane gaming keyboards", Slug = "keyboards", ImageUrl = "/images/categories/keyboards.jpg", DisplayOrder = 2 },
                    new() { Name = "Headsets", Description = "Immersive surround-sound gaming headsets", Slug = "headsets", ImageUrl = "/images/categories/headsets.jpg", DisplayOrder = 3 },
                    new() { Name = "Monitors", Description = "High refresh rate gaming monitors", Slug = "monitors", ImageUrl = "/images/categories/monitors.jpg", DisplayOrder = 4 },
                    new() { Name = "RGB Lighting", Description = "LED strips, ambient lighting, and RGB accessories", Slug = "rgb-lighting", ImageUrl = "/images/categories/rgb.jpg", DisplayOrder = 5 },
                    new() { Name = "Gaming Chairs", Description = "Ergonomic racing-style gaming chairs", Slug = "gaming-chairs", ImageUrl = "/images/categories/chairs.jpg", DisplayOrder = 6 },
                    new() { Name = "Controllers", Description = "Wired and wireless gaming controllers", Slug = "controllers", ImageUrl = "/images/categories/controllers.jpg", DisplayOrder = 7 },
                    new() { Name = "Accessories", Description = "Mousepads, cable managers, desk mats, and more", Slug = "accessories", ImageUrl = "/images/categories/accessories.jpg", DisplayOrder = 8 },
                };
                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Products.Any())
            {
                var categories = context.Categories.ToList();
                int miceId    = categories.First(c => c.Slug == "gaming-mice").Id;
                int kbId      = categories.First(c => c.Slug == "keyboards").Id;
                int hsId      = categories.First(c => c.Slug == "headsets").Id;
                int monId     = categories.First(c => c.Slug == "monitors").Id;
                int rgbId     = categories.First(c => c.Slug == "rgb-lighting").Id;
                int chairId   = categories.First(c => c.Slug == "gaming-chairs").Id;
                int ctrlId    = categories.First(c => c.Slug == "controllers").Id;
                int accId     = categories.First(c => c.Slug == "accessories").Id;

                var products = new List<Product>
                {
                    new() { Name = "NX Phantom Pro Mouse", Price = 89.99m, DiscountPercentage = 15, Stock = 50, CategoryId = miceId, Brand = "NexusGear", IsFeatured = true, IsNewArrival = true, Slug = "nx-phantom-pro-mouse", ImageUrl = "/images/products/mouse1.jpg", Description = "16000 DPI optical sensor, 6 programmable buttons, per-key RGB lighting. Built for FPS dominance.", Sku = "NG-M001" },
                    new() { Name = "SilentStorm Wireless Mouse", Price = 69.99m, Stock = 35, CategoryId = miceId, Brand = "SilentStorm", IsNewArrival = true, Slug = "silentstorm-wireless-mouse", ImageUrl = "/images/products/mouse2.jpg", Description = "Ultra-silent wireless mouse with 70-hour battery life, 12000 DPI sensor and tri-mode connectivity.", Sku = "NG-M002" },
                    new() { Name = "Vortex Entry Mouse", Price = 24.99m, DiscountPercentage = 20, Stock = 100, CategoryId = miceId, Brand = "Vortex", Slug = "vortex-entry-mouse", ImageUrl = "/images/products/mouse3.jpg", Description = "Budget-friendly 6400 DPI gaming mouse with 4 adjustable DPI settings and RGB underglow.", Sku = "NG-M003" },
                    new() { Name = "ArcLight TKL Keyboard", Price = 139.99m, DiscountPercentage = 10, Stock = 40, CategoryId = kbId, Brand = "ArcLight", IsFeatured = true, IsNewArrival = true, Slug = "arclight-tkl-keyboard", ImageUrl = "/images/products/kb1.jpg", Description = "Tenkeyless mechanical keyboard with Cherry MX Red switches, per-key RGB, aluminium top frame.", Sku = "NG-K001" },
                    new() { Name = "PulseMech Full Keyboard", Price = 109.99m, Stock = 60, CategoryId = kbId, Brand = "PulseMech", Slug = "pulsemech-full-keyboard", ImageUrl = "/images/products/kb2.jpg", Description = "Full-size mechanical keyboard with Blue switches, multimedia knob, PBT keycaps.", Sku = "NG-K002" },
                    new() { Name = "NeonType RGB Keyboard", Price = 79.99m, DiscountPercentage = 25, Stock = 45, CategoryId = kbId, Brand = "NeonType", IsFeatured = true, Slug = "neontype-rgb-keyboard", ImageUrl = "/images/products/kb3.jpg", Description = "60% compact RGB keyboard with hot-swap sockets, gasket-mounted PCB for whisper-quiet typing.", Sku = "NG-K003" },
                    new() { Name = "OmniSound 7.1 Headset", Price = 109.99m, DiscountPercentage = 10, Stock = 30, CategoryId = hsId, Brand = "OmniSound", IsFeatured = true, IsNewArrival = true, Slug = "omnisound-71-headset", ImageUrl = "/images/products/hs1.jpg", Description = "Virtual 7.1 surround sound, retractable noise-cancelling mic, memory foam earcups, 50mm drivers.", Sku = "NG-H001" },
                    new() { Name = "DeepBass Pro Headset", Price = 69.99m, Stock = 55, CategoryId = hsId, Brand = "DeepBass", Slug = "deepbass-pro-headset", ImageUrl = "/images/products/hs2.jpg", Description = "40mm neodymium drivers delivering deep bass. Foldable, multi-platform compatible.", Sku = "NG-H002" },
                    new() { Name = "PrismView 165Hz Monitor", Price = 319.99m, DiscountPercentage = 5, Stock = 20, CategoryId = monId, Brand = "PrismView", IsFeatured = true, IsNewArrival = true, Slug = "prismview-165hz-monitor", ImageUrl = "/images/products/mon1.jpg", Description = "27-inch QHD IPS, 165Hz, 1ms GTG, FreeSync Premium Pro. Perfect colours and blazing speed.", Sku = "NG-V001" },
                    new() { Name = "UltraEdge 240Hz Monitor", Price = 549.99m, Stock = 15, CategoryId = monId, Brand = "UltraEdge", IsFeatured = true, Slug = "ultraedge-240hz-monitor", ImageUrl = "/images/products/mon2.jpg", Description = "24.5-inch FHD TN, 240Hz, 0.5ms GTG, G-Sync compatible. The ultimate competitive display.", Sku = "NG-V002" },
                    new() { Name = "AuraStrip Pro 3m", Price = 44.99m, DiscountPercentage = 30, Stock = 80, CategoryId = rgbId, Brand = "AuraStrip", IsNewArrival = true, Slug = "aurastrip-pro-3m", ImageUrl = "/images/products/rgb1.jpg", Description = "3-metre addressable RGB LED strip, 16M colours, music sync, app control, corner connectors included.", Sku = "NG-L001" },
                    new() { Name = "HaloBar Ambient Kit", Price = 84.99m, DiscountPercentage = 15, Stock = 40, CategoryId = rgbId, Brand = "HaloBar", IsFeatured = true, Slug = "halobar-ambient-kit", ImageUrl = "/images/products/rgb2.jpg", Description = "Complete desk ambient lighting kit: 4 LED bars, corner pieces, controller, USB-C power.", Sku = "NG-L002" },
                    new() { Name = "ThroneX Ultra Chair", Price = 429.99m, DiscountPercentage = 10, Stock = 12, CategoryId = chairId, Brand = "ThroneX", IsFeatured = true, IsNewArrival = true, Slug = "thronex-ultra-chair", ImageUrl = "/images/products/chair1.jpg", Description = "Premium racing-style chair: 4D armrests, 165° recline, cold-cure foam, Class 4 hydraulic piston.", Sku = "NG-C001" },
                    new() { Name = "NovaSeat Mesh Chair", Price = 269.99m, Stock = 0, CategoryId = chairId, Brand = "NovaSeat", Slug = "novaseat-mesh-chair", ImageUrl = "/images/products/chair2.jpg", Description = "Breathable full-mesh gaming chair with adjustable lumbar, headrest, and tilt-lock mechanism.", Sku = "NG-C002" },
                    new() { Name = "ProPad Elite Controller", Price = 74.99m, DiscountPercentage = 12, Stock = 35, CategoryId = ctrlId, Brand = "ProPad", IsFeatured = true, Slug = "propad-elite-controller", ImageUrl = "/images/products/ctrl1.jpg", Description = "Wireless controller with hall-effect triggers, 1000Hz polling, 40-hour battery, PC & console support.", Sku = "NG-G001" },
                    new() { Name = "DeskMat XL RGB", Price = 34.99m, Stock = 60, CategoryId = accId, Brand = "NexusGear", IsNewArrival = true, Slug = "deskmat-xl-rgb", ImageUrl = "/images/products/mat1.jpg", Description = "900×400mm XXL RGB desk mat with stitched edges, non-slip base, and custom lighting zones.", Sku = "NG-A001" },
                };

                context.Products.AddRange(products);
                await context.SaveChangesAsync();
            }

            if (!context.Testimonials.Any())
            {
                context.Testimonials.AddRange(
                    new Testimonial { AuthorName = "Alex Carter", AuthorTitle = "Pro Gamer", Content = "NexusGear has the absolute best selection I've found anywhere online. The ArcLight keyboard changed everything about how I play.", Rating = 5 },
                    new Testimonial { AuthorName = "Sarah Mitchell", AuthorTitle = "Content Creator", Content = "The HaloBar ambient kit looks insane on camera. Shipping was fast, packaging was premium. 10/10 will buy again.", Rating = 5 },
                    new Testimonial { AuthorName = "James Wu", AuthorTitle = "Esports Coach", Content = "Ordered keyboards for my entire team. Great bulk pricing, arrived in perfect condition. Customer support was excellent.", Rating = 5 },
                    new Testimonial { AuthorName = "Maria Santos", AuthorTitle = "Casual Gamer", Content = "Easy to find exactly what I need, filters are great, checkout was smooth. My setup looks like a battlestation now!", Rating = 4 }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
