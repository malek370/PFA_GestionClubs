# Kafka Integration avec le Pattern Decorator

## Vue d'ensemble

Cette implémentation utilise le **pattern Decorator** pour ajouter la fonctionnalité de publication Kafka aux services existants sans modifier leur code original. Cela permet de respecter le principe Open/Closed (ouvert à l'extension, fermé à la modification).

## Architecture

### Pattern Decorator

Le pattern Decorator enveloppe les services originaux et ajoute la fonctionnalité de publication Kafka :

```
IClubServices (Interface)
	├── ClubServices (Implémentation de base)
	└── ClubServicesKafkaDecorator (Décorateur avec Kafka)

IAnnoucementService (Interface)
	├── AnnoucementService (Implémentation de base)
	└── AnnouncementServiceKafkaDecorator (Décorateur avec Kafka)

IEventService (Interface)
	├── EventService (Implémentation de base)
	└── EventServiceKafkaDecorator (Décorateur avec Kafka)
```

## Fonctionnalités implémentées

### 1. Publication de nouveaux clubs
- **Topic Kafka**: `clubs-topic`
- **Event**: `ClubCreatedEvent`
- **Décorateur**: `ClubServicesKafkaDecorator`
- Publié lors de la création d'un nouveau club via `CreateClub()`

### 2. Publication de nouvelles annonces
- **Topic Kafka**: `announcements-topic`
- **Event**: `AnnouncementCreatedEvent`
- **Décorateur**: `AnnouncementServiceKafkaDecorator`
- Publié lors de la création d'une nouvelle annonce via `CreateAnnoucement()`

### 3. Publication de nouveaux événements
- **Topic Kafka**: `events-topic`
- **Event**: `EventCreatedEvent`
- **Décorateur**: `EventServiceKafkaDecorator`
- Publié lors de la création d'un nouvel événement via `AddEvent()`

## Structure des événements

### ClubCreatedEvent
```csharp
{
	ClubId: int,
	ClubName: string,
	Description: string,
	PresidentEmail: string?,
	CreatedAt: DateTime
}
```

### AnnouncementCreatedEvent
```csharp
{
	AnnouncementId: int,
	Title: string,
	Content: string,
	ClubName: string,
	CreatedAt: DateTime
}
```

### EventCreatedEvent
```csharp
{
	EventId: int,
	Title: string,
	Description: string,
	Location: string?,
	StartDate: DateTime,
	ClubName: string,
	CreatedAt: DateTime
}
```

## Configuration

Dans `appsettings.Development.json` :

```json
{
  "Kafka": {
	"BootstrapServers": "localhost:9094",
	"ClubsTopic": "clubs-topic",
	"AnnouncementsTopic": "announcements-topic",
	"EventsTopic": "events-topic"
  }
}
```

## Injection de dépendances

Les décorateurs sont enregistrés dans `Program.cs` :

```csharp
// Clubs
builder.Services.AddScoped<ClubServices>();
builder.Services.AddScoped<IClubServices>(sp =>
	new ClubServicesKafkaDecorator(
		sp.GetRequiredService<ClubServices>(),
		sp.GetRequiredService<IKafkaProducer>(),
		sp.GetRequiredService<IOptions<KafkaOptions>>()
	));

// Announcements
builder.Services.AddScoped<AnnoucementService>();
builder.Services.AddScoped<IAnnoucementService>(sp =>
	new AnnouncementServiceKafkaDecorator(
		sp.GetRequiredService<AnnoucementService>(),
		sp.GetRequiredService<IKafkaProducer>(),
		sp.GetRequiredService<IOptions<KafkaOptions>>()
	));

// Events
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<IEventService>(sp =>
	new EventServiceKafkaDecorator(
		sp.GetRequiredService<EventService>(),
		sp.GetRequiredService<IKafkaProducer>(),
		sp.GetRequiredService<IOptions<KafkaOptions>>()
	));
```

## Avantages du Pattern Decorator

1. **Séparation des préoccupations** : La logique métier reste séparée de la logique de publication Kafka
2. **Réutilisabilité** : Les services de base peuvent être utilisés avec ou sans Kafka
3. **Testabilité** : Les services peuvent être testés indépendamment de Kafka
4. **Extensibilité** : D'autres décorateurs peuvent être ajoutés facilement (logging, caching, etc.)
5. **Respect du principe Open/Closed** : Extension sans modification du code existant

## Utilisation

Les décorateurs sont transparents pour les contrôleurs. Le code existant n'a pas besoin d'être modifié :

```csharp
// Dans un contrôleur
public class ClubsController : ControllerBase
{
	private readonly IClubServices _clubServices;

	public ClubsController(IClubServices clubServices)
	{
		_clubServices = clubServices; // Injecte automatiquement le décorateur
	}

	[HttpPost]
	public async Task<IActionResult> CreateClub(CreateClubDTO dto)
	{
		var club = await _clubServices.CreateClub(dto);
		// Le message Kafka est publié automatiquement
		return Ok(club);
	}
}
```

## Fichiers créés/modifiés

### Nouveaux fichiers
- `GestionClubs\Application\Events\ClubCreatedEvent.cs`
- `GestionClubs\Application\Events\AnnouncementCreatedEvent.cs`
- `GestionClubs\Application\Events\EventCreatedEvent.cs`
- `GestionClubs\Infrastructure\Decorators\AnnouncementServiceKafkaDecorator.cs`
- `GestionClubs\Infrastructure\Decorators\EventServiceKafkaDecorator.cs`

### Fichiers modifiés
- `GestionClubs\Infrastructure\Decorators\ClubServicesKafkaDecorator.cs`
- `GestionClubs\Infrastructure\Kafka\KafkaOptions.cs`
- `GestionClubs\GestionClubs\Program.cs`
- `GestionClubs\GestionClubs\appsettings.Development.json`

## Tests

Pour tester la publication Kafka :

1. Démarrer Kafka et créer les topics :
```bash
kafka-topics.sh --create --topic clubs-topic --bootstrap-server localhost:9094
kafka-topics.sh --create --topic announcements-topic --bootstrap-server localhost:9094
kafka-topics.sh --create --topic events-topic --bootstrap-server localhost:9094
```

2. Écouter les messages :
```bash
kafka-console-consumer.sh --topic clubs-topic --bootstrap-server localhost:9094 --from-beginning
kafka-console-consumer.sh --topic announcements-topic --bootstrap-server localhost:9094 --from-beginning
kafka-console-consumer.sh --topic events-topic --bootstrap-server localhost:9094 --from-beginning
```

3. Créer un club/annonce/événement via l'API et observer les messages dans Kafka
