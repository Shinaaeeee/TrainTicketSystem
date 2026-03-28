using System;
using System.Linq;
using TrainTicketSystem.Models;

namespace TrainTicketSystem.Models
{
    public static class DbInitializer
    {
        public static void Initialize(TrainTicketDbContext context)
        {
            // Kiểm tra xem database đã có dữ liệu chưa (nếu bảng User hoặc Train có dữ liệu thì skip)
            if (context.Users.Any() || context.Trains.Any())
            {
                return; // DB has been seeded
            }

            // 1. Seed Users
            var users = new User[]
            {
                new User { Username = "admin", Password = "123", FullName = "Admin User", Email = "admin@example.com", Phone = "0123456789", Role = "admin" },
                new User { Username = "customer", Password = "123", FullName = "Nguyễn Văn A", Email = "customer@example.com", Phone = "0987654321", Role = "customer" }
            };
            context.Users.AddRange(users);
            context.SaveChanges();

            // 2. Seed SeatTypes
            var seatTypes = new SeatType[]
            {
                new SeatType { TypeName = "Ngồi Mềm Điều Hòa", PriceMultiplier = 1.0m },
                new SeatType { TypeName = "Giường Nằm Khoang 6", PriceMultiplier = 1.5m },
                new SeatType { TypeName = "Giường Nằm Khoang 4", PriceMultiplier = 2.0m }
            };
            context.SeatTypes.AddRange(seatTypes);
            context.SaveChanges();

            // 3. Seed Trains
            var trains = new Train[]
            {
                new Train { TrainName = "SE1", Capacity = 30 },
                new Train { TrainName = "SE2", Capacity = 30 },
                new Train { TrainName = "SE3", Capacity = 30 },
                new Train { TrainName = "SE4", Capacity = 30 }
            };
            context.Trains.AddRange(trains);
            context.SaveChanges();

            // 4. Seed Routes
            var routes = new Route[]
            {
                new Route { StartStation = "Hà Nội", EndStation = "Sài Gòn", DistanceKm = 1726 },
                new Route { StartStation = "Sài Gòn", EndStation = "Hà Nội", DistanceKm = 1726 },
                new Route { StartStation = "Hà Nội", EndStation = "Đà Nẵng", DistanceKm = 791 },
                new Route { StartStation = "Đà Nẵng", EndStation = "Hà Nội", DistanceKm = 791 }
            };
            context.Routes.AddRange(routes);
            context.SaveChanges();

            // 5. Seed Schedules
            var schedules = new Schedule[]
            {
                new Schedule { TrainId = trains[0].TrainId, RouteId = routes[0].RouteId, DepartureTime = DateTime.Now.AddDays(1).AddHours(6), ArrivalTime = DateTime.Now.AddDays(2).AddHours(14), Price = 1000000 },
                new Schedule { TrainId = trains[1].TrainId, RouteId = routes[1].RouteId, DepartureTime = DateTime.Now.AddDays(1).AddHours(8), ArrivalTime = DateTime.Now.AddDays(2).AddHours(16), Price = 1000000 },
                new Schedule { TrainId = trains[2].TrainId, RouteId = routes[2].RouteId, DepartureTime = DateTime.Now.AddDays(2).AddHours(10), ArrivalTime = DateTime.Now.AddDays(3).AddHours(1), Price = 500000 },
                new Schedule { TrainId = trains[3].TrainId, RouteId = routes[3].RouteId, DepartureTime = DateTime.Now.AddDays(3).AddHours(12), ArrivalTime = DateTime.Now.AddDays(4).AddHours(3), Price = 500000 }
            };
            context.Schedules.AddRange(schedules);
            context.SaveChanges();

            // 6. Seed Seats cho các Train
            // Mỗi train sẽ có 10 ghế "Ngồi Mềm", 10 ghế "Khoang 6", 10 ghế "Khoang 4"
            var seats = new System.Collections.Generic.List<Seat>();
            foreach(var train in trains)
            {
                int seatIndex = 1;
                foreach(var type in seatTypes)
                {
                    for(int i = 1; i <= 10; i++)
                    {
                        var seatAlias = type.TypeName!.Contains("Ngồi") ? "NM" : (type.TypeName.Contains("6") ? "GN6" : "GN4");
                        seats.Add(new Seat
                        {
                            TrainId = train.TrainId,
                            SeatTypeId = type.SeatTypeId,
                            SeatNumber = $"{seatAlias}-{seatIndex++}",
                            SeatHoldStatus = "Available",
                            HoldExpiredAt = null,
                            HeldByUserId = null
                        });
                    }
                }
            }
            context.Seats.AddRange(seats);
            context.SaveChanges();
        }
    }
}
