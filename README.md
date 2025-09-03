# Ninja MMORPG - Unity + ASP.NET Core

A comprehensive multiplayer online RPG ninja-themed game featuring turn-based combat, character progression, and immersive ninja community mechanics.

## 🎮 Game Overview

**Ninja MMORPG** is a text-based MMORPG inspired by Naruto, featuring:

- **5 Core Stats** (Max: 250,000 each) - Strength, Intelligence, Speed, Willpower
- **4 Combat Stats** (Max: 500,000 each) - Ninjutsu, Genjutsu, Bukijutsu, Taijutsu
- **5 Starting Villages** - Hidden Leaf, Stone, Mist, Sand, Cloud
- **Turn-Based Combat** - 100 Action Points per turn, 5x8 grid battlefield
- **Medical Ninja System** - 4-tier healing progression
- **Clan System** - Village-based clans with leadership hierarchy
- **Mission System** - D-S rank missions with progression requirements
- **Daily Lottery** - Global lottery with 100% prize pool distribution

## 🏗️ Architecture

### Backend (ASP.NET Core)
- **API Layer** - RESTful endpoints with JWT authentication
- **SignalR Hubs** - Real-time communication for combat, chat, and village activities
- **Entity Framework Core** - Data access with SQL Server
- **Rate Limiting** - Comprehensive API protection
- **AutoMapper** - DTO mapping and validation

### Unity Client
- **Cross-Platform** - Android and iOS support
- **MVVM Pattern** - Clean separation of concerns
- **Singleton Managers** - Centralized game state management
- **Event-Driven Architecture** - Reactive UI updates
- **SignalR Integration** - Real-time multiplayer communication

## 📁 Project Structure

```
NinjaMMORPG/
├── Backend/                          # ASP.NET Core Backend
│   ├── NinjaMMORPG.API/            # Web API and SignalR Hubs
│   ├── NinjaMMORPG.Core/           # Domain Entities and Enums
│   ├── NinjaMMORPG.Infrastructure/ # Data Access and EF Core
│   └── NinjaMMORPG.Tests/          # Unit and Integration Tests
├── Unity/                           # Unity Client Project
│   ├── Assets/
│   │   ├── Scripts/
│   │   │   ├── Core/               # Game Manager and Core Systems
│   │   │   ├── Managers/            # System Managers
│   │   │   ├── Models/              # Data Models
│   │   │   ├── UI/                  # User Interface Components
│   │   │   ├── Combat/              # Combat System
│   │   │   └── Enums/               # Game Enums
│   │   ├── Prefabs/                 # Reusable Game Objects
│   │   ├── Scenes/                  # Game Scenes
│   │   └── Resources/               # Game Assets
│   └── Packages/                    # Unity Package Dependencies
└── README.md                        # Project Documentation
```

## 🚀 Getting Started

### Prerequisites
- **.NET 8.0 SDK**
- **SQL Server** (LocalDB or full instance)
- **Unity 2022.3 LTS** or later
- **Visual Studio 2022** or **VS Code**

### Backend Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/ninja-mmorpg.git
   cd ninja-mmorpg
   ```

2. **Restore NuGet packages**
   ```bash
   cd Backend
   dotnet restore
   ```

3. **Update database connection string**
   - Edit `Backend/NinjaMMORPG.API/appsettings.json`
   - Update `DefaultConnection` to point to your SQL Server instance

4. **Create and update database**
   ```bash
   cd NinjaMMORPG.API
   dotnet ef database update
   ```

5. **Run the backend**
   ```bash
   dotnet run
   ```

   The API will be available at `https://localhost:5001`
   Swagger documentation at `https://localhost:5001/swagger`

### Unity Client Setup

1. **Open Unity Hub**
2. **Add the Unity project folder**
3. **Open the project in Unity 2022.3 LTS**
4. **Install required packages** (should auto-install from manifest.json)
5. **Update server URL** in NetworkManager if needed
6. **Build and run** on your target platform

## 🎯 Development Phases

### Phase 1: Core Foundation (Weeks 1-3) ✅
- [x] ASP.NET Core backend setup
- [x] Entity Framework models and database
- [x] Unity project structure
- [x] Basic game managers
- [x] Character creation system
- [x] Village selection (5 villages)

### Phase 2: Hospital and Medical System (Weeks 4-6)
- [ ] Hospital auto-transport at 0 HP
- [ ] 2-minute recovery timer
- [ ] Medical ninja healing system
- [ ] Med nin ranking progression
- [ ] Healing effectiveness scaling

### Phase 3: Mission and Quest Systems (Weeks 7-10)
- [ ] Mission Hall with rank-gated missions
- [ ] Mission Journal and tracking
- [ ] Quest system (main story, side quests, events)
- [ ] Level-scaling rewards
- [ ] Always-active regeneration system

### Phase 4: Social Systems and Economy (Weeks 11-14)
- [ ] Clan system with Kage creation rights
- [ ] Silent Division elite groups
- [ ] Banking system (pocket/bank separation)
- [ ] Village defection and Outlaw ranks
- [ ] 6-slot equipment system

### Phase 5: Advanced Features and Polish (Weeks 15-16)
- [ ] Global daily lottery system
- [ ] Sensei system for mentoring
- [ ] Comprehensive chat systems
- [ ] PvP systems and tournaments
- [ ] Final balancing and testing

## 🎮 Core Game Systems

### Character Progression
- **Starting Stats**: All stats begin at 1
- **Core Stats**: Strength, Intelligence, Speed, Willpower (Max: 250,000)
- **Combat Stats**: Ninjutsu, Genjutsu, Bukijutsu, Taijutsu (Max: 500,000)
- **Ranks**: Student → Genin → Chunin → Jounin → Special Jounin
- **Special Ranks**: Kage, Elder (appointment only)

### Combat System
- **Action Points**: 100 AP per turn
- **Grid Battlefield**: 5x8 rectangular grid
- **Deterministic Combat**: No RNG except tied Speed initiative
- **Damage Formula**: Attack Stat - Defense Stat = Final Damage (minimum 1)
- **Elemental System**: Rock-paper-scissors mechanics

### Village System
- **Hidden Leaf Village**: Forest/nature theme
- **Stone Village**: Mountain/earth theme
- **Mist Village**: Water/coastal theme
- **Sand Village**: Desert theme
- **Cloud Village**: Sky/lightning theme

### Medical Ninja System
- **Novice Medic**: Basic healing abilities
- **Field Medic**: Improved healing efficiency
- **Master Medic**: Advanced healing techniques
- **Legendary Healer**: Maximum healing power

## 🔧 Technical Features

### Backend Features
- **JWT Authentication** - Secure user authentication
- **SignalR Hubs** - Real-time multiplayer communication
- **Rate Limiting** - API abuse prevention
- **Entity Framework Core** - Efficient data access
- **AutoMapper** - Clean DTO mapping
- **Serilog** - Comprehensive logging
- **Swagger** - API documentation

### Unity Features
- **Cross-Platform Support** - Android and iOS
- **MVVM Architecture** - Clean code structure
- **Event-Driven UI** - Reactive user interface
- **Singleton Pattern** - Centralized game management
- **Coroutine System** - Efficient timing and animations
- **PlayerPrefs** - Local data persistence

## 🧪 Testing

### Backend Testing
```bash
cd Backend/NinjaMMORPG.Tests
dotnet test
```

### Unity Testing
- Use Unity's built-in Test Framework
- Run tests from Test Runner window
- Coverage includes unit tests and integration tests

## 📱 Platform Support

- **Android**: API Level 24+ (Android 7.0+)
- **iOS**: iOS 12.0+
- **Development**: Windows, macOS, Linux

## 🚀 Deployment

### Backend Deployment
- **Azure App Service** - Recommended for production
- **Docker** - Containerized deployment
- **Azure SQL Database** - Managed SQL Server
- **Azure SignalR Service** - Scalable real-time communication

### Unity Client Deployment
- **Google Play Store** - Android distribution
- **Apple App Store** - iOS distribution
- **Unity Cloud Build** - Automated builds
- **Firebase** - Analytics and crash reporting

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch** (`git checkout -b feature/amazing-feature`)
3. **Commit your changes** (`git commit -m 'Add amazing feature'`)
4. **Push to the branch** (`git push origin feature/amazing-feature`)
5. **Open a Pull Request**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Naruto** - Inspiration for the ninja theme
- **Unity Technologies** - Game development platform
- **Microsoft** - ASP.NET Core framework
- **Open Source Community** - Various libraries and tools

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/ninja-mmorpg/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/ninja-mmorpg/discussions)
- **Wiki**: [Project Wiki](https://github.com/yourusername/ninja-mmorpg/wiki)

## 🔮 Future Roadmap

- **Mobile Optimization** - Enhanced performance for mobile devices
- **Advanced Combat** - More jutsu types and combinations
- **Guild Wars** - Inter-village conflicts and alliances
- **Seasonal Events** - Limited-time content and rewards
- **Monetization** - Cosmetic items and convenience features
- **Cross-Platform Play** - Unity WebGL and desktop support

---

**Happy coding, ninja developers! 🥷✨**
