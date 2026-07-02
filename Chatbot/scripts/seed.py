"""Seed the UniClubs chatbot database with data from seedGestionClub.sql.

Data mirrors the GestionClubs .NET service database exactly:
  - 40 users, 10 clubs, 80 events, 80+ participants entries,
    30+ announcements, 60+ adhesions.

NOT seeded from SQL (no matching table):
  - FAQs  — platform-help texts, kept as static data below.

Run:
    cd Chatbot
    $env:PYTHONPATH="."; python scripts/seed.py
"""
from datetime import datetime, timezone

from app.db import Base, SessionLocal, engine
from app.models import Adhesion, Announcement, Club, Event, FAQ, Member, User
from app.services.faq_indexing_service import faq_indexing_service
from app.services.knowledge_service import knowledge_service


def _dt(s: str) -> datetime:
    """Parse a SQL datetime string into a UTC-aware datetime."""
    return datetime.fromisoformat(s).replace(tzinfo=timezone.utc)


# --------------------------------------------------------------------------- #
# Users (40 total — batch 1: IDs 1-20, batch 2: IDs 21-40)                   #
# --------------------------------------------------------------------------- #
USERS = [
    # Batch 1
    (1,  "alice.martin@gmail.com",      "Alice",    "Martin",   "2023-01-10 08:00:00"),
    (2,  "bob.johnson@gmail.com",       "Bob",      "Johnson",  "2023-01-12 09:15:00"),
    (3,  "claire.dupont@yahoo.com",     "Claire",   "Dupont",   "2023-01-15 10:30:00"),
    (4,  "david.smith@outlook.com",     "David",    "Smith",    "2023-02-01 11:00:00"),
    (5,  "emma.wilson@gmail.com",       "Emma",     "Wilson",   "2023-02-05 08:45:00"),
    (6,  "felix.bernard@gmail.com",     "Felix",    "Bernard",  "2023-02-10 09:00:00"),
    (7,  "grace.lee@hotmail.com",       "Grace",    "Lee",      "2023-02-14 14:00:00"),
    (8,  "henry.clark@gmail.com",       "Henry",    "Clark",    "2023-02-20 16:30:00"),
    (9,  "isabella.moore@yahoo.com",    "Isabella", "Moore",    "2023-03-01 10:00:00"),
    (10, "james.taylor@gmail.com",      "James",    "Taylor",   "2023-03-05 11:15:00"),
    (11, "karen.white@outlook.com",     "Karen",    "White",    "2023-03-10 13:00:00"),
    (12, "liam.harris@gmail.com",       "Liam",     "Harris",   "2023-03-15 09:30:00"),
    (13, "mia.thomas@gmail.com",        "Mia",      "Thomas",   "2023-03-20 08:00:00"),
    (14, "noah.jackson@hotmail.com",    "Noah",     "Jackson",  "2023-04-01 10:45:00"),
    (15, "olivia.walker@gmail.com",     "Olivia",   "Walker",   "2023-04-05 12:00:00"),
    (16, "peter.hall@yahoo.com",        "Peter",    "Hall",     "2023-04-10 14:30:00"),
    (17, "quinn.allen@gmail.com",       "Quinn",    "Allen",    "2023-04-15 09:00:00"),
    (18, "rachel.young@outlook.com",    "Rachel",   "Young",    "2023-04-20 11:00:00"),
    (19, "samuel.king@gmail.com",       "Samuel",   "King",     "2023-05-01 08:30:00"),
    (20, "tara.scott@gmail.com",        "Tara",     "Scott",    "2023-05-05 10:00:00"),
    # Batch 2
    (21, "amara.diallo@gmail.com",      "Amara",    "Diallo",   "2023-05-10 08:00:00"),
    (22, "ben.nguyen@yahoo.com",        "Ben",      "Nguyen",   "2023-05-11 09:10:00"),
    (23, "chloe.lambert@outlook.com",   "Chloe",    "Lambert",  "2023-05-13 10:20:00"),
    (24, "diego.reyes@gmail.com",       "Diego",    "Reyes",    "2023-05-15 11:30:00"),
    (25, "elena.petrov@gmail.com",      "Elena",    "Petrov",   "2023-05-17 08:45:00"),
    (26, "farid.hassan@hotmail.com",    "Farid",    "Hassan",   "2023-05-19 09:00:00"),
    (27, "gina.romano@gmail.com",       "Gina",     "Romano",   "2023-05-21 14:00:00"),
    (28, "hiro.tanaka@yahoo.com",       "Hiro",     "Tanaka",   "2023-05-23 16:30:00"),
    (29, "ingrid.berg@outlook.com",     "Ingrid",   "Berg",     "2023-05-25 10:00:00"),
    (30, "julius.okafor@gmail.com",     "Julius",   "Okafor",   "2023-05-27 11:15:00"),
    (31, "katya.sorel@gmail.com",       "Katya",    "Sorel",    "2023-05-29 13:00:00"),
    (32, "leon.fischer@outlook.com",    "Leon",     "Fischer",  "2023-06-01 09:30:00"),
    (33, "maya.patel@gmail.com",        "Maya",     "Patel",    "2023-06-03 08:00:00"),
    (34, "noel.santos@hotmail.com",     "Noel",     "Santos",   "2023-06-05 10:45:00"),
    (35, "ophelia.crane@gmail.com",     "Ophelia",  "Crane",    "2023-06-07 12:00:00"),
    (36, "pierre.leblanc@yahoo.com",    "Pierre",   "Leblanc",  "2023-06-09 14:30:00"),
    (37, "quentin.marsh@gmail.com",     "Quentin",  "Marsh",    "2023-06-11 09:00:00"),
    (38, "rosa.ferreira@outlook.com",   "Rosa",     "Ferreira", "2023-06-13 11:00:00"),
    (39, "stefan.muller@gmail.com",     "Stefan",   "Muller",   "2023-06-15 08:30:00"),
    (40, "yuki.nakamura@gmail.com",     "Yuki",     "Nakamura", "2023-06-17 10:00:00"),
]

# --------------------------------------------------------------------------- #
# Clubs (10 total — batch 1: IDs 1-5, batch 2: IDs 6-10)                     #
# (id, name, description, documents_list, created_at)                         #
# --------------------------------------------------------------------------- #
CLUBS = [
    (1, "Tech Innovators Club",
     "A community for tech enthusiasts who love building, hacking, and discussing the latest in software and hardware.",
     ["charter.pdf", "rules.pdf"], "2023-01-20 09:00:00"),
    (2, "Photography Society",
     "Dedicated to the art and craft of photography — from film to digital, portrait to landscape.",
     ["guidelines.pdf"], "2023-01-25 10:00:00"),
    (3, "Book Lovers Circle",
     "A monthly reading group exploring fiction, non-fiction, classics, and contemporary literature.",
     ["reading_list.pdf", "membership_form.pdf"], "2023-02-01 11:00:00"),
    (4, "Outdoor Adventures Club",
     "Organizing hikes, camping trips, and outdoor sports events for nature lovers of all skill levels.",
     ["safety_guidelines.pdf", "waiver.pdf"], "2023-02-15 08:00:00"),
    (5, "Chess & Strategy Guild",
     "For players of all levels who enjoy chess, go, and other strategy games. Weekly matches and tournaments.",
     ["tournament_rules.pdf"], "2023-03-01 09:00:00"),
    (6, "Film & Cinema Club",
     "Celebrating the art of film — from silent classics to modern arthouse. Weekly screenings and lively discussions.",
     ["screening_schedule.pdf", "code_of_conduct.pdf"], "2023-05-20 09:00:00"),
    (7, "Sustainable Living Network",
     "A community focused on eco-friendly lifestyles, zero-waste tips, urban gardening, and green initiatives.",
     ["mission_statement.pdf", "resources.pdf"], "2023-05-25 10:00:00"),
    (8, "Creative Writing Workshop",
     "A safe, inspiring space for writers of all genres — fiction, poetry, screenwriting, and memoir.",
     ["submission_guidelines.pdf"], "2023-06-01 11:00:00"),
    (9, "Robotics & Makers Club",
     "Hands-on building, tinkering, and engineering. From Arduino projects to full autonomous robots.",
     ["safety_rules.pdf", "equipment_list.pdf"], "2023-06-10 08:00:00"),
    (10, "Yoga & Mindfulness Collective",
     "A welcoming community for all levels of yoga practice, meditation, and holistic well-being.",
     ["waiver.pdf", "class_schedule.pdf"], "2023-06-15 09:00:00"),
]

# --------------------------------------------------------------------------- #
# Members                                                                      #
# (club_id, user_id, post_in_club_int, created_at)                            #
# post_in_club: 0=Member, 1=Moderator, 2=President                            #
# --------------------------------------------------------------------------- #
_POST = {0: "Member", 1: "Moderator", 2: "President"}

MEMBERS = [
    # Tech Innovators Club (1)
    (1, 1,  2, "2023-01-20 09:05:00"),
    (1, 2,  1, "2023-01-21 10:00:00"),
    (1, 3,  0, "2023-01-22 11:00:00"),
    (1, 4,  0, "2023-01-25 12:00:00"),
    (1, 5,  0, "2023-02-01 09:00:00"),
    # Photography Society (2)
    (2, 6,  2, "2023-01-25 10:05:00"),
    (2, 7,  1, "2023-01-26 11:00:00"),
    (2, 8,  0, "2023-01-27 12:00:00"),
    (2, 9,  0, "2023-02-01 09:00:00"),
    (2, 10, 0, "2023-02-05 10:00:00"),
    # Book Lovers Circle (3)
    (3, 11, 2, "2023-02-01 11:05:00"),
    (3, 12, 1, "2023-02-02 12:00:00"),
    (3, 13, 0, "2023-02-03 09:00:00"),
    (3, 14, 0, "2023-02-10 10:00:00"),
    (3, 1,  0, "2023-02-15 11:00:00"),
    # Outdoor Adventures Club (4)
    (4, 15, 2, "2023-02-15 08:05:00"),
    (4, 16, 1, "2023-02-16 09:00:00"),
    (4, 17, 0, "2023-02-20 10:00:00"),
    (4, 18, 0, "2023-03-01 09:00:00"),
    (4, 19, 0, "2023-03-05 10:00:00"),
    # Chess & Strategy Guild (5)
    (5, 20, 2, "2023-03-01 09:05:00"),
    (5, 4,  1, "2023-03-02 10:00:00"),
    (5, 5,  0, "2023-03-05 11:00:00"),
    (5, 10, 0, "2023-03-10 12:00:00"),
    (5, 14, 0, "2023-03-15 09:00:00"),
    # Film & Cinema Club (6)
    (6, 21, 2, "2023-05-20 09:05:00"),
    (6, 22, 1, "2023-05-21 10:00:00"),
    (6, 23, 0, "2023-05-22 11:00:00"),
    (6, 24, 0, "2023-05-25 12:00:00"),
    (6, 25, 0, "2023-06-01 09:00:00"),
    # Sustainable Living Network (7)
    (7, 26, 2, "2023-05-25 10:05:00"),
    (7, 27, 1, "2023-05-26 11:00:00"),
    (7, 28, 0, "2023-05-27 12:00:00"),
    (7, 29, 0, "2023-06-01 09:00:00"),
    (7, 30, 0, "2023-06-05 10:00:00"),
    # Creative Writing Workshop (8)
    (8, 31, 2, "2023-06-01 11:05:00"),
    (8, 32, 1, "2023-06-02 12:00:00"),
    (8, 33, 0, "2023-06-03 09:00:00"),
    (8, 34, 0, "2023-06-10 10:00:00"),
    (8, 21, 0, "2023-06-15 11:00:00"),
    # Robotics & Makers Club (9)
    (9, 35, 2, "2023-06-10 08:05:00"),
    (9, 36, 1, "2023-06-11 09:00:00"),
    (9, 37, 0, "2023-06-12 10:00:00"),
    (9, 38, 0, "2023-06-15 09:00:00"),
    (9, 39, 0, "2023-06-18 10:00:00"),
    # Yoga & Mindfulness Collective (10)
    (10, 40, 2, "2023-06-15 09:05:00"),
    (10, 24, 1, "2023-06-16 10:00:00"),
    (10, 25, 0, "2023-06-17 11:00:00"),
    (10, 30, 0, "2023-06-20 12:00:00"),
    (10, 34, 0, "2023-06-22 09:00:00"),
]

# --------------------------------------------------------------------------- #
# Events (80 total)                                                            #
# (id, club_id, title, description, is_public, location, start_date,          #
#  tags_csv, created_at)                                                       #
# is_public: SQL uses 0/1 ints; converted to bool below.                      #
# tags_csv: comma-separated string; split to list below.                      #
# --------------------------------------------------------------------------- #
EVENTS = [
    # --- Tech Innovators Club (1) ---
    (1,  1, "Web Dev Workshop",
     "A hands-on workshop covering modern web development with React and Node.js. Bring your laptop!",
     1, "Room 101, Tech Hub Building", "2023-03-15 14:00:00",
     "webdev,react,nodejs", "2023-03-01 09:00:00"),
    (2,  1, "AI & Machine Learning Meetup",
     "Monthly meetup to discuss the latest trends in AI and ML. Lightning talks and open Q&A.",
     1, "Innovation Lab, Floor 3", "2023-04-10 18:00:00",
     "ai,machinelearning,python", "2023-03-20 10:00:00"),
    (3,  1, "Hackathon Planning Session",
     "Internal planning session for our upcoming 24-hour hackathon. Members only.",
     0, "Conference Room B", "2023-04-20 10:00:00",
     "hackathon,planning", "2023-04-05 09:00:00"),
    # --- Photography Society (2) ---
    (4,  2, "Golden Hour Photo Walk",
     "Join us for a relaxed walk around the city at golden hour to capture stunning urban shots.",
     1, "City Central Park — Main Entrance", "2023-03-25 17:30:00",
     "photowalk,urban,goldenHour", "2023-03-10 08:00:00"),
    (5,  2, "Portrait Photography Workshop",
     "Learn lighting setups, posing techniques, and post-processing tips for stunning portraits.",
     0, "Studio 4, Arts Center", "2023-04-08 10:00:00",
     "portrait,studio,workshop", "2023-03-22 10:00:00"),
    (6,  2, "Annual Photo Exhibition",
     "Showcasing the best works from our members over the past year. Open to the public!",
     1, "City Gallery, Main Hall", "2023-05-20 10:00:00",
     "exhibition,gallery,showcase", "2023-04-01 09:00:00"),
    # --- Book Lovers Circle (3) ---
    (7,  3, 'Monthly Book Discussion: "The Midnight Library"',
     "This month we explore Matt Haig's bestseller. Come prepared with your thoughts and questions!",
     0, "Central Library, Meeting Room 2", "2023-03-18 15:00:00",
     "fiction,discussion,mattHaig", "2023-03-05 09:00:00"),
    (8,  3, "Author Q&A Evening",
     "A special evening with a local author discussing their writing process and latest novel.",
     1, "Bookshop Café, Downtown", "2023-04-15 19:00:00",
     "author,qa,writing", "2023-04-01 10:00:00"),
    (9,  3, "Summer Reading Kickoff",
     "Come pick your summer reading list, share recommendations, and enjoy refreshments.",
     1, "Riverside Terrace", "2023-06-01 16:00:00",
     "summer,reading,recommendations", "2023-05-10 09:00:00"),
    # --- Outdoor Adventures Club (4) ---
    (10, 4, "Spring Hiking Trip",
     "A moderate 12km hike through the national park trails. Suitable for beginners and beyond.",
     1, "National Park — North Trailhead", "2023-04-22 07:00:00",
     "hiking,nature,spring", "2023-04-01 08:00:00"),
    (11, 4, "Overnight Camping Weekend",
     "Two days, one night under the stars. Gear checklist and carpooling details sent to members.",
     0, "Lakeside Campground", "2023-05-12 08:00:00",
     "camping,overnight,nature", "2023-04-20 09:00:00"),
    (12, 4, "Rock Climbing Introduction",
     "A beginner-friendly intro to rock climbing with certified instructors. Gear provided.",
     1, "Rocky Ridge Climbing Center", "2023-05-28 09:00:00",
     "climbing,beginner,sports", "2023-05-01 08:00:00"),
    # --- Chess & Strategy Guild (5) ---
    (13, 5, "Weekly Chess Night",
     "Casual chess evening open to all skill levels. Clocks and boards provided. Beginners welcome!",
     1, "The Queens Gambit Café", "2023-03-22 19:00:00",
     "chess,casual,weekly", "2023-03-15 09:00:00"),
    (14, 5, "Spring Chess Tournament",
     "Our annual spring tournament with Swiss-system format. Prizes for top 3 finishers.",
     1, "Community Centre, Hall A", "2023-04-29 10:00:00",
     "tournament,chess,competitive", "2023-04-05 10:00:00"),
    (15, 5, "Strategy Board Game Night",
     "A members-only evening featuring Catan, Terraforming Mars, Wingspan, and more.",
     0, "Members Lounge", "2023-05-05 18:00:00",
     "boardgames,strategy,fun", "2023-04-25 09:00:00"),
    # --- Film & Cinema Club (6) ---
    (16, 6, "Screening: Kurosawa Double Feature",
     'We screen "Rashomon" and "Seven Samurai" back-to-back with a short intermission and group discussion.',
     1, "Cinéclub Theatre, Screen 2", "2023-07-08 17:00:00",
     "kurosawa,classic,japanese", "2023-06-20 09:00:00"),
    (17, 6, "Documentary Night: Planet Earth III",
     "Members-only preview screening of the new Planet Earth series followed by a panel discussion.",
     0, "Members Screening Room", "2023-07-22 19:00:00",
     "documentary,nature,bbc", "2023-07-01 10:00:00"),
    (18, 6, "Screenwriting 101 Workshop",
     "An introduction to three-act structure, character arcs, and dialogue. Open to all levels.",
     1, "Arts Faculty, Room 204", "2023-08-05 10:00:00",
     "screenwriting,workshop,film", "2023-07-15 09:00:00"),
    # --- Sustainable Living Network (7) ---
    (19, 7, "Zero Waste Cooking Class",
     "Chef Gina leads a hands-on class showing how to cook delicious meals with minimal food waste.",
     1, "Community Kitchen, Level 1", "2023-07-15 11:00:00",
     "zerowaste,cooking,sustainability", "2023-07-01 08:00:00"),
    (20, 7, "Urban Rooftop Garden Tour",
     "A private tour of three urban rooftop gardens around the city. Transport arranged for members.",
     0, "Meeting Point: City Hall Steps", "2023-07-29 09:00:00",
     "garden,urban,tour", "2023-07-10 09:00:00"),
    (21, 7, "Swap & Repair Fair",
     "Bring clothes, electronics, and household items to swap or get repaired by our volunteer fixers.",
     1, "Central Square, Outdoor Pavilion", "2023-08-19 10:00:00",
     "swap,repair,circular,community", "2023-07-25 10:00:00"),
    # --- Creative Writing Workshop (8) ---
    (22, 8, "Flash Fiction Sprint",
     "Write a complete story in under 500 words in 45 minutes. Prompts provided. Sharing optional!",
     0, "The Write Café, Back Room", "2023-07-12 18:00:00",
     "flashfiction,writing,sprint", "2023-07-01 09:00:00"),
    (23, 8, "Poetry Open Mic Night",
     "Share your poems with a warm, supportive audience. All styles and experience levels welcome.",
     1, "Harbour View Lounge", "2023-07-27 19:30:00",
     "poetry,openmic,performance", "2023-07-10 10:00:00"),
    (24, 8, "Manuscript Feedback Session",
     "Bring up to 10 pages of your work-in-progress for structured peer feedback. Members only.",
     0, "Library Study Suite B", "2023-08-10 14:00:00",
     "feedback,manuscript,critique", "2023-07-28 09:00:00"),
    # --- Robotics & Makers Club (9) ---
    (25, 9, "Arduino Beginner Bootcamp",
     "A full-day introduction to Arduino microcontrollers. Build your first blinking LED circuit and beyond.",
     1, "Makerspace Lab, Workshop Floor", "2023-07-20 09:00:00",
     "arduino,beginner,electronics", "2023-07-05 08:00:00"),
    (26, 9, "Robot Sumo Competition",
     "Teams of two compete with their custom-built robots in our tabletop sumo arena. Prizes awarded!",
     1, "Engineering Faculty, Hall B", "2023-08-12 10:00:00",
     "robotics,competition,sumo", "2023-07-20 09:00:00"),
    (27, 9, "3D Printing Deep Dive",
     "Members-only session covering advanced slicing techniques, material science, and troubleshooting.",
     0, "Makerspace Lab, Print Suite", "2023-08-26 10:00:00",
     "3dprinting,makers,advanced", "2023-08-05 08:00:00"),
    # --- Yoga & Mindfulness Collective (10) ---
    (28, 10, "Sunrise Flow Yoga — Outdoor Session",
     "Start your weekend with an energising vinyasa flow in the park. Mats provided, all levels welcome.",
     1, "Botanical Garden, East Lawn", "2023-07-16 06:30:00",
     "yoga,outdoor,sunrise,vinyasa", "2023-07-05 09:00:00"),
    (29, 10, "Guided Meditation & Sound Bath",
     "A deeply restorative 90-minute session with singing bowls and breath-work. Members only.",
     0, "Wellness Studio, Floor 2", "2023-08-03 19:00:00",
     "meditation,soundbath,breathwork", "2023-07-20 10:00:00"),
    (30, 10, "Yin Yoga & Journaling Retreat",
     "A half-day retreat combining slow yin yoga with reflective journaling exercises. Open to all.",
     1, "Riverside Wellness Centre", "2023-08-27 09:00:00",
     "yin,yoga,journaling,retreat", "2023-08-05 09:00:00"),
    # --- Tech Innovators Club — batch 3 (1) ---
    (31, 1, "24-Hour Hackathon",
     "Our flagship annual hackathon. Build anything in 24 hours — solo or in teams of up to 4. Food and drinks provided.",
     1, "Tech Hub Building, All Floors", "2023-07-15 09:00:00",
     "hackathon,coding,teamwork", "2023-06-20 09:00:00"),
    (32, 1, "Open Source Contribution Day",
     "Pick a popular open source project and spend the day making your first contribution. Mentors available.",
     1, "Innovation Lab, Floor 3", "2023-08-05 10:00:00",
     "opensource,github,contribution", "2023-07-15 09:00:00"),
    (33, 1, "DevOps & Cloud Workshop",
     "Hands-on workshop covering Docker, Kubernetes, and deploying to AWS. Laptops required.",
     0, "Conference Room A", "2023-08-19 10:00:00",
     "devops,cloud,docker,aws", "2023-07-30 09:00:00"),
    (34, 1, "Tech Career Panel",
     "Five industry professionals share their journeys into tech and answer your career questions live.",
     1, "Auditorium, Ground Floor", "2023-09-09 14:00:00",
     "career,panel,networking", "2023-08-20 09:00:00"),
    (35, 1, "Year-End Demo Day",
     "Members present their side projects and apps built over the past year. Voting for best project!",
     1, "Innovation Lab, Main Hall", "2023-12-02 13:00:00",
     "demo,showcase,projects", "2023-11-01 09:00:00"),
    # --- Photography Society — batch 3 (2) ---
    (36, 2, "Night Photography Masterclass",
     "Learn long-exposure techniques, light painting, and astrophotography fundamentals. Bring a tripod.",
     0, "Rooftop Terrace, Arts Centre", "2023-07-14 21:00:00",
     "nightphotography,longexposure,astro", "2023-07-01 09:00:00"),
    (37, 2, "Street Photography Walk — Old Quarter",
     "A guided 3-hour walk through the old quarter capturing candid street moments. All cameras welcome.",
     1, "Old Quarter — Clock Tower Meeting Point", "2023-08-12 09:00:00",
     "street,candid,photowalk", "2023-07-25 09:00:00"),
    (38, 2, "Lightroom Editing Bootcamp",
     "A full afternoon mastering Lightroom — cataloguing, colour grading, and exporting for print and web.",
     0, "Computer Lab 3, Arts Faculty", "2023-09-02 13:00:00",
     "lightroom,editing,postprocessing", "2023-08-15 09:00:00"),
    (39, 2, "Nature & Wildlife Photography Trip",
     "A day trip to the national reserve to photograph birds, insects, and landscapes. Carpooling arranged.",
     0, "National Nature Reserve — Visitor Centre", "2023-09-23 07:00:00",
     "wildlife,nature,birds,daytrip", "2023-09-01 09:00:00"),
    (40, 2, "Members Portfolio Review",
     "Submit 5–10 of your best images for constructive group critique. A great way to grow your eye.",
     0, "Studio 4, Arts Center", "2023-10-14 14:00:00",
     "portfolio,critique,review", "2023-09-25 09:00:00"),
    # --- Book Lovers Circle — batch 3 (3) ---
    (41, 3, 'Monthly Discussion: "Tomorrow, and Tomorrow, and Tomorrow"',
     "Gabrielle Zevin's critically acclaimed novel about friendship, creativity, and video games is our pick.",
     0, "Central Library, Meeting Room 2", "2023-07-15 15:00:00",
     "fiction,discussion,gabrielleZevin", "2023-07-01 09:00:00"),
    (42, 3, "Genre Deep Dive: Science Fiction",
     "A special themed session exploring the history and best works of science fiction literature.",
     1, "Bookshop Café, Downtown", "2023-08-19 15:00:00",
     "scifi,genre,discussion", "2023-08-01 09:00:00"),
    (43, 3, "Speed Dating for Books",
     "Each member has 3 minutes to pitch their favourite book to the group. Leave with a reading list!",
     1, "Riverside Terrace", "2023-09-16 15:00:00",
     "recommendations,fun,pitching", "2023-09-01 09:00:00"),
    (44, 3, "Banned Books Reading Night",
     "We read and discuss excerpts from historically banned books and explore why they were censored.",
     0, "Central Library, Meeting Room 2", "2023-10-07 18:00:00",
     "bannedbooks,history,censorship", "2023-09-20 09:00:00"),
    (45, 3, "Christmas Reads Swap",
     "Bring a wrapped book, take a wrapped book. Share what you are reading this festive season.",
     1, "Bookshop Café, Downtown", "2023-12-09 16:00:00",
     "christmas,swap,giftideas", "2023-11-20 09:00:00"),
    # --- Outdoor Adventures Club — batch 3 (4) ---
    (46, 4, "Kayaking Day Trip",
     "A guided half-day kayaking session on the river. No experience needed. Life jackets provided.",
     1, "River Dock, South Marina", "2023-07-08 08:00:00",
     "kayaking,water,adventure", "2023-06-20 09:00:00"),
    (47, 4, "Mountain Bike Trail Ride",
     "Intermediate trail ride through forest paths. Helmets mandatory, bikes available to rent nearby.",
     0, "Forest Park — East Entrance", "2023-08-06 08:30:00",
     "mountainbike,cycling,trail", "2023-07-20 09:00:00"),
    (48, 4, "Survival Skills Workshop",
     "Learn fire-starting, shelter-building, navigation by stars, and foraging basics with our expert guide.",
     0, "National Park, Campsite B", "2023-09-03 09:00:00",
     "survival,skills,wilderness", "2023-08-15 09:00:00"),
    (49, 4, "Autumn Foraging Walk",
     "Guided walk to identify edible mushrooms, berries, and plants. Expert botanist leads the group.",
     1, "Woodland Reserve — West Gate", "2023-10-01 09:30:00",
     "foraging,autumn,nature,plants", "2023-09-15 09:00:00"),
    (50, 4, "End of Year Bonfire & Stargazing",
     "Wrap up the outdoor season with a bonfire, hot drinks, and amateur astronomy. All welcome.",
     1, "Lakeside Campground", "2023-11-18 18:00:00",
     "bonfire,stargazing,social", "2023-11-01 09:00:00"),
    # --- Chess & Strategy Guild — batch 3 (5) ---
    (51, 5, "Blindfold Chess Exhibition",
     "Watch our top players compete blindfolded simultaneously on multiple boards. Free entry.",
     1, "Community Centre, Hall A", "2023-07-08 15:00:00",
     "chess,blindfold,exhibition", "2023-06-22 09:00:00"),
    (52, 5, "Chess Tactics Crash Course",
     "Two-hour crash course covering pins, forks, skewers, and back rank threats. All levels welcome.",
     0, "Members Lounge", "2023-08-05 11:00:00",
     "chess,tactics,learning", "2023-07-20 09:00:00"),
    (53, 5, "Inter-Club Friendly Match",
     "A friendly match against the University Chess Team. Come support our players!",
     1, "University Sports Hall, Room 12", "2023-09-16 14:00:00",
     "chess,friendly,match,university", "2023-09-01 09:00:00"),
    (54, 5, "Go & Shogi Introduction Night",
     "Explore two classic strategy games from East Asia. Boards and rulebooks provided for beginners.",
     1, "The Queens Gambit Café", "2023-10-07 18:00:00",
     "go,shogi,strategy,boardgames", "2023-09-25 09:00:00"),
    (55, 5, "Annual Grand Tournament",
     "Our biggest event of the year — 32-player knockout chess tournament. Register early, spots are limited.",
     1, "Community Centre, Hall A", "2023-11-25 09:00:00",
     "tournament,chess,annual,competitive", "2023-11-01 09:00:00"),
    # --- Film & Cinema Club — batch 3 (6) ---
    (56, 6, "New Wave Cinema Night",
     'A curated evening of French New Wave classics: Godard\'s "Breathless" and Truffaut\'s "The 400 Blows".',
     1, "Cinéclub Theatre, Screen 2", "2023-08-12 18:00:00",
     "frenchnewwave,godard,truffaut,classic", "2023-07-28 09:00:00"),
    (57, 6, "Film Score & Soundtrack Evening",
     "We explore how music shapes cinema — from Bernard Herrmann to Hans Zimmer. Listening session included.",
     0, "Members Screening Room", "2023-09-09 19:00:00",
     "soundtrack,filmscore,music,cinema", "2023-08-25 09:00:00"),
    (58, 6, "Short Film Submission Showcase",
     "Members submit short films (under 10 min) for screening and peer feedback. Submissions open now.",
     0, "Arts Faculty, Room 204", "2023-10-14 16:00:00",
     "shortfilm,showcase,feedback,members", "2023-09-20 09:00:00"),
    (59, 6, "Horror Film Marathon — Halloween Special",
     "A curated all-night marathon of classic and modern horror films. Costumes encouraged!",
     1, "Cinéclub Theatre, Screen 1", "2023-10-28 20:00:00",
     "horror,halloween,marathon,spooky", "2023-10-05 09:00:00"),
    (60, 6, "Year in Review: Best Films of 2023",
     "Members vote and debate the best films released this year. Bring your hot takes!",
     0, "Members Screening Room", "2023-12-16 17:00:00",
     "review,2023,yearend,debate", "2023-12-01 09:00:00"),
    # --- Sustainable Living Network — batch 3 (7) ---
    (61, 7, "DIY Natural Cleaning Products",
     "Learn to make effective cleaning products using vinegar, baking soda, and essential oils. Kits provided.",
     1, "Community Kitchen, Level 1", "2023-08-26 11:00:00",
     "diy,cleaning,natural,zerowaste", "2023-08-10 09:00:00"),
    (62, 7, "Solar Energy Info Session",
     "An expert explains how home solar panels work, costs, subsidies available, and how to get started.",
     1, "Public Library, Conference Room", "2023-09-16 14:00:00",
     "solar,energy,renewable,homeowner", "2023-09-01 09:00:00"),
    (63, 7, "Seed Saving & Autumn Planting Workshop",
     "Learn which seeds to save from summer crops and how to prepare your garden for spring.",
     0, "Community Garden, Plot Area", "2023-10-07 10:00:00",
     "seeds,gardening,autumn,planting", "2023-09-20 09:00:00"),
    (64, 7, "Sustainable Fashion Swap",
     "Bring up to 5 clothing items in good condition, swap them for something new-to-you. No money needed.",
     1, "Central Square, Outdoor Pavilion", "2023-10-21 10:00:00",
     "fashion,swap,sustainable,clothing", "2023-10-05 09:00:00"),
    (65, 7, "End of Year Impact Review",
     "Members review our collective environmental impact for 2023 and vote on initiatives for 2024.",
     0, "Community Kitchen, Level 1", "2023-12-09 14:00:00",
     "review,impact,planning,2024", "2023-11-20 09:00:00"),
    # --- Creative Writing Workshop — batch 3 (8) ---
    (66, 8, "World Building Masterclass",
     "Build rich, believable worlds for your fiction — geography, politics, culture, magic systems, and more.",
     0, "Library Study Suite B", "2023-08-19 14:00:00",
     "worldbuilding,fantasy,fiction,craft", "2023-08-05 09:00:00"),
    (67, 8, "Dialogue Workshop",
     "Study what makes dialogue crackle on the page. Exercises, examples, and live rewrites.",
     0, "The Write Café, Back Room", "2023-09-09 15:00:00",
     "dialogue,craft,writing,workshop", "2023-08-25 09:00:00"),
    (68, 8, "Non-Fiction & Personal Essay Evening",
     "A session dedicated to the craft of personal essays and narrative non-fiction. Guest writer joins us.",
     1, "Harbour View Lounge", "2023-10-05 19:00:00",
     "nonfiction,essay,memoir,craft", "2023-09-20 09:00:00"),
    (69, 8, "NaNoWriMo Kick-Off Party",
     "Celebrate the start of National Novel Writing Month with us — goal-setting, sprints, and solidarity!",
     1, "The Write Café, Back Room", "2023-11-01 18:00:00",
     "nanowrimo,novel,writing,community", "2023-10-20 09:00:00"),
    (70, 8, "Literary Magazine Launch Night",
     "The official launch of our first annual literary magazine. Readings from published contributors.",
     1, "Harbour View Lounge", "2023-12-07 19:00:00",
     "magazine,launch,reading,celebration", "2023-11-25 09:00:00"),
    # --- Robotics & Makers Club — batch 3 (9) ---
    (71, 9, "Soldering Basics Workshop",
     "Learn to solder safely and confidently. We will build a simple FM radio kit from scratch.",
     0, "Makerspace Lab, Electronics Bench", "2023-08-05 10:00:00",
     "soldering,electronics,makers,beginner", "2023-07-20 09:00:00"),
    (72, 9, "Raspberry Pi Home Automation",
     "Use a Raspberry Pi to automate lights, sensors, and alerts in your home. Components provided.",
     0, "Makerspace Lab, Workshop Floor", "2023-09-02 10:00:00",
     "raspberrypi,automation,iot,makers", "2023-08-15 09:00:00"),
    (73, 9, "Open Maker Showcase",
     "Members display their latest projects to the public. A great chance to inspire and get feedback.",
     1, "Engineering Faculty, Atrium", "2023-10-07 11:00:00",
     "showcase,makers,projects,public", "2023-09-20 09:00:00"),
    (74, 9, "Drone Building & Flying Session",
     "Build a mini drone from a kit, then take it for its first flight in our indoor arena. Safety briefing included.",
     0, "Makerspace Lab, Main Hall", "2023-11-04 10:00:00",
     "drones,building,flying,makers", "2023-10-20 09:00:00"),
    (75, 9, "End of Year Build Challenge",
     "Teams of 2 have 4 hours to build something creative from a mystery box of components. Judged on creativity!",
     0, "Makerspace Lab, Workshop Floor", "2023-12-09 10:00:00",
     "challenge,build,creative,teamwork", "2023-11-25 09:00:00"),
    # --- Yoga & Mindfulness Collective — batch 3 (10) ---
    (76, 10, "Partner Yoga Workshop",
     "Explore trust, communication, and balance through fun partner yoga poses. No partner needed to join!",
     1, "Wellness Studio, Floor 2", "2023-08-13 10:00:00",
     "partneryoga,trust,balance,fun", "2023-08-01 09:00:00"),
    (77, 10, "Breath & Body: Pranayama Deep Dive",
     "A members-only session dedicated entirely to pranayama breathing techniques and their benefits.",
     0, "Wellness Studio, Floor 2", "2023-09-10 09:00:00",
     "pranayama,breathing,mindfulness,yoga", "2023-08-28 09:00:00"),
    (78, 10, "Autumn Equinox Outdoor Practice",
     "Celebrate the autumn equinox with a grounding outdoor yoga and meditation practice at sunrise.",
     1, "Botanical Garden, East Lawn", "2023-09-23 06:30:00",
     "equinox,outdoor,sunrise,seasonal", "2023-09-10 09:00:00"),
    (79, 10, "Restorative Yoga & Hot Cocoa Evening",
     "A deeply nourishing restorative session followed by warm drinks and a mindful journaling prompt.",
     0, "Wellness Studio, Floor 2", "2023-11-11 18:00:00",
     "restorative,cosy,autumn,wellness", "2023-10-28 09:00:00"),
    (80, 10, "New Year Intentions Setting Retreat",
     "Half-day retreat combining yoga, breathwork, and guided journaling to set intentions for 2024.",
     1, "Riverside Wellness Centre", "2023-12-30 09:00:00",
     "newyear,intentions,retreat,2024", "2023-12-10 09:00:00"),
]

# --------------------------------------------------------------------------- #
# Event participants (event_id, user_id)                                       #
# --------------------------------------------------------------------------- #
EVENT_PARTICIPANTS = [
    # Events 1-15 (batch 1)
    (1,1),(1,2),(1,3),(1,4),(1,5),
    (2,1),(2,2),(2,4),(2,5),(2,10),
    (3,1),(3,2),(3,3),
    (4,6),(4,7),(4,8),(4,9),(4,10),(4,3),
    (5,6),(5,7),(5,8),
    (6,6),(6,7),(6,8),(6,9),(6,10),(6,1),(6,2),(6,15),
    (7,11),(7,12),(7,13),(7,14),(7,1),
    (8,11),(8,12),(8,13),(8,1),(8,7),(8,15),
    (9,11),(9,12),(9,13),(9,14),(9,1),(9,5),
    (10,15),(10,16),(10,17),(10,18),(10,19),(10,6),(10,12),
    (11,15),(11,16),(11,17),(11,18),
    (12,15),(12,16),(12,17),(12,19),(12,3),(12,8),
    (13,20),(13,4),(13,5),(13,10),(13,14),(13,2),(13,9),
    (14,20),(14,4),(14,5),(14,10),(14,14),
    (15,20),(15,4),(15,5),(15,10),
    # Events 16-30 (batch 2)
    (16,21),(16,22),(16,23),(16,24),(16,25),(16,31),
    (17,21),(17,22),(17,23),
    (18,21),(18,22),(18,24),(18,25),(18,33),(18,37),
    (19,26),(19,27),(19,28),(19,29),(19,30),(19,23),
    (20,26),(20,27),(20,28),(20,29),
    (21,26),(21,27),(21,28),(21,29),(21,30),(21,40),(21,35),
    (22,31),(22,32),(22,33),(22,34),(22,21),
    (23,31),(23,32),(23,33),(23,34),(23,21),(23,27),(23,40),
    (24,31),(24,32),(24,33),(24,34),
    (25,35),(25,36),(25,37),(25,38),(25,39),(25,22),(25,29),
    (26,35),(26,36),(26,37),(26,38),(26,39),(26,24),
    (27,35),(27,36),(27,37),(27,39),
    (28,40),(28,24),(28,25),(28,30),(28,34),(28,28),(28,23),
    (29,40),(29,24),(29,25),(29,30),
    (30,40),(30,24),(30,25),(30,30),(30,34),(30,27),(30,33),
    # Events 31-80 (batch 3)
    (31,1),(31,2),(31,3),(31,4),(31,5),(31,22),(31,37),
    (32,1),(32,2),(32,4),(32,5),(32,29),
    (33,1),(33,2),(33,3),(33,5),
    (34,1),(34,2),(34,3),(34,4),(34,5),(34,22),(34,24),(34,33),
    (35,1),(35,2),(35,3),(35,4),(35,5),(35,37),
    (36,6),(36,7),(36,8),(36,9),(36,10),
    (37,6),(37,7),(37,8),(37,9),(37,10),(37,23),(37,33),
    (38,6),(38,7),(38,8),(38,10),
    (39,6),(39,7),(39,8),(39,9),
    (40,6),(40,7),(40,8),(40,9),(40,10),
    (41,11),(41,12),(41,13),(41,14),(41,1),
    (42,11),(42,12),(42,13),(42,14),(42,1),(42,22),(42,25),
    (43,11),(43,12),(43,13),(43,14),(43,1),(43,31),
    (44,11),(44,12),(44,13),(44,14),
    (45,11),(45,12),(45,13),(45,14),(45,1),(45,27),(45,40),
    (46,15),(46,16),(46,17),(46,18),(46,19),(46,26),(46,30),
    (47,15),(47,16),(47,17),(47,19),
    (48,15),(48,16),(48,17),(48,18),(48,19),
    (49,15),(49,16),(49,17),(49,18),(49,19),(49,27),(49,34),
    (50,15),(50,16),(50,17),(50,18),(50,19),(50,40),(50,6),(50,1),
    (51,20),(51,4),(51,5),(51,10),(51,14),(51,2),(51,22),
    (52,20),(52,4),(52,5),(52,10),(52,14),
    (53,20),(53,4),(53,5),(53,10),(53,14),(53,1),(53,3),
    (54,20),(54,4),(54,5),(54,10),(54,14),(54,21),(54,32),
    (55,20),(55,4),(55,5),(55,10),(55,14),(55,22),(55,28),(55,37),
    (56,21),(56,22),(56,23),(56,24),(56,25),(56,31),(56,11),
    (57,21),(57,22),(57,23),(57,25),
    (58,21),(58,22),(58,23),(58,24),(58,25),
    (59,21),(59,22),(59,23),(59,24),(59,25),(59,13),(59,33),(59,5),
    (60,21),(60,22),(60,23),(60,24),(60,25),
    (61,26),(61,27),(61,28),(61,29),(61,30),(61,40),(61,15),
    (62,26),(62,27),(62,28),(62,29),(62,30),(62,16),(62,35),
    (63,26),(63,27),(63,28),(63,29),(63,30),
    (64,26),(64,27),(64,28),(64,29),(64,30),(64,13),(64,40),(64,7),
    (65,26),(65,27),(65,28),(65,29),(65,30),
    (66,31),(66,32),(66,33),(66,34),(66,21),
    (67,31),(67,32),(67,33),(67,34),(67,21),
    (68,31),(68,32),(68,33),(68,34),(68,21),(68,11),(68,23),
    (69,31),(69,32),(69,33),(69,34),(69,21),(69,12),(69,1),
    (70,31),(70,32),(70,33),(70,34),(70,21),(70,22),(70,6),(70,11),
    (71,35),(71,36),(71,37),(71,38),(71,39),
    (72,35),(72,36),(72,37),(72,38),(72,39),(72,4),(72,22),
    (73,35),(73,36),(73,37),(73,38),(73,39),(73,1),(73,24),(73,29),
    (74,35),(74,36),(74,37),(74,39),
    (75,35),(75,36),(75,37),(75,38),(75,39),
    (76,40),(76,24),(76,25),(76,30),(76,34),(76,28),(76,27),
    (77,40),(77,24),(77,25),(77,30),(77,34),
    (78,40),(78,24),(78,25),(78,30),(78,34),(78,26),(78,13),
    (79,40),(79,24),(79,25),(79,30),(79,34),
    (80,40),(80,24),(80,25),(80,30),(80,34),(80,27),(80,33),(80,1),
]

# --------------------------------------------------------------------------- #
# Announcements                                                                #
# (club_id, title, content, is_public_int, created_at)                        #
# --------------------------------------------------------------------------- #
ANNOUNCEMENTS = [
    # Tech Innovators Club (1) — batches 1 & 3
    (1, "Welcome to Tech Innovators Club!",
     "We are excited to launch our club. Check the events calendar for our first workshop this March.",
     1, "2023-01-20 09:30:00"),
    (1, "New Slack Workspace",
     "We now have a dedicated Slack workspace for members. Invite link sent to your registered email.",
     0, "2023-02-01 10:00:00"),
    (1, "Hackathon Date Confirmed",
     "Our first 24-hour hackathon is confirmed for July 2023. Start thinking about your team and project ideas!",
     1, "2023-04-20 11:00:00"),
    (1, "Hackathon Registration Now Open",
     "Sign up for our 24-hour hackathon on July 15th. Teams of 1–4, any stack, any idea. Limited spots!",
     1, "2023-06-20 09:30:00"),
    (1, "New Resource Library Launched",
     "Members now have access to our curated library of tutorials, courses, and coding challenges via the portal.",
     0, "2023-07-05 10:00:00"),
    (1, "Call for Mentors",
     "Are you a senior developer or engineer? We are looking for mentors to support junior members. Reply to this post.",
     0, "2023-08-01 09:00:00"),
    (1, "DevOps Workshop Recap",
     "Missed the DevOps workshop? Notes, slides, and a recording are now available in the members portal.",
     0, "2023-08-25 10:00:00"),
    (1, "Demo Day: Submit Your Project",
     "Year-end Demo Day is December 2nd. Submit your project title and a one-line description by November 20th.",
     0, "2023-11-05 09:00:00"),
    # Photography Society (2) — batches 1 & 3
    (2, "Welcome to the Photography Society!",
     "Glad to have you here! Our first photo walk is planned for late March — stay tuned.",
     1, "2023-01-25 10:30:00"),
    (2, "Equipment Lending Library",
     "Members can now borrow lenses and tripods. Check the members portal to reserve gear.",
     0, "2023-02-15 09:00:00"),
    (2, "Photo of the Month Competition",
     "Submit your best shot for April's theme: \"Reflections\". Voting is open to all members.",
     0, "2023-04-01 08:00:00"),
    (2, "July Theme: Long Exposure",
     "This month's photo challenge theme is long exposure. Submit your best shot by July 28th.",
     0, "2023-07-01 09:00:00"),
    (2, "New Tripod & Filter Sets Available",
     "Two new tripod kits and a set of ND filters have been added to the equipment lending library.",
     0, "2023-07-20 10:00:00"),
    (2, "Street Photography Walk — Spots Remaining",
     "Only 4 spots left on the August street photography walk. Sign up through the events page.",
     1, "2023-08-05 09:00:00"),
    (2, "Exhibition Prints Now on Display",
     "Prints from the May annual exhibition are now on permanent display in the Arts Centre corridor.",
     1, "2023-09-01 09:00:00"),
    (2, "Winter Photo Challenge Announced",
     "Our winter challenge theme is \"Stillness\". Submissions open November 1st to December 1st.",
     1, "2023-10-20 09:00:00"),
    # Book Lovers Circle (3) — batches 1 & 3
    (3, "April Reading Pick Announced",
     "Our next book is \"Pachinko\" by Min Jin Lee. Copies available at the library or online.",
     1, "2023-03-20 10:00:00"),
    (3, "New Meeting Location",
     "Starting next month we will be meeting at the Riverside Terrace instead of the library.",
     0, "2023-04-01 09:00:00"),
    (3, "July Book: Voting Results",
     '"Tomorrow, and Tomorrow, and Tomorrow" won the vote for July! Copies available at the front desk.',
     0, "2023-07-03 09:00:00"),
    (3, "We Have a Goodreads Group",
     "Join our Goodreads group to track your reading, see member reviews, and vote on upcoming picks.",
     0, "2023-07-20 10:00:00"),
    (3, "Guest Author Confirmed for October",
     "A local prize-winning novelist has confirmed attendance at our October session. Details to follow.",
     1, "2023-09-15 09:00:00"),
    (3, "Banned Books Night — Pre-Reading Suggestions",
     'For October\'s banned books night, consider reading "1984", "The Handmaid\'s Tale", or "Lolita" in advance.',
     0, "2023-09-22 09:00:00"),
    (3, "December Swap: Wrapping Rules",
     "For the Christmas book swap, please wrap your book and add a one-sentence note without naming the title.",
     0, "2023-11-25 09:00:00"),
    # Outdoor Adventures Club (4) — batches 1 & 3
    (4, "Safety Guidelines Updated",
     "Please review the updated safety guidelines document before attending any outdoor events.",
     0, "2023-03-01 08:00:00"),
    (4, "Gear Swap Event Coming Soon",
     "Got outdoor gear you no longer use? Our gear swap event will be announced next month.",
     1, "2023-04-15 10:00:00"),
    (4, "Hiking Trip Spots Filling Up",
     "Only 5 spots remaining for the Spring Hiking Trip! Register through the events page.",
     1, "2023-04-10 09:00:00"),
    (4, "Kayaking Trip: What to Bring",
     "For the July kayaking trip bring sun cream, a change of clothes, water shoes if you have them, and a snack.",
     0, "2023-07-03 08:00:00"),
    (4, "Partnership with Local Bike Shop",
     "Members now receive 15% off rentals and repairs at Trailblazer Bikes. Show your member card.",
     0, "2023-07-25 09:00:00"),
    (4, "Survival Workshop: Limited to 10 Members",
     "The September survival skills workshop is limited to 10 participants. First come first served.",
     0, "2023-08-20 08:00:00"),
    (4, "Autumn Foraging: Expert Guide Confirmed",
     "Dr. Lena Voss, botanist and author of \"Wild Larder\", will lead our autumn foraging walk.",
     1, "2023-09-18 09:00:00"),
    (4, "Gear Donation Drive",
     "Clearing out your garage? Donate unused outdoor gear to the club. We will clean, check, and lend it out.",
     1, "2023-10-10 09:00:00"),
    # Chess & Strategy Guild (5) — batches 1 & 3
    (5, "Tournament Registration Open",
     "Register for the Spring Chess Tournament before April 22nd. Limited to 32 players.",
     1, "2023-04-05 10:30:00"),
    (5, "New Study Group Forming",
     "Interested in improving your chess? We are forming a weekly study group. Contact Tara to join.",
     0, "2023-04-10 09:00:00"),
    (5, "Blindfold Exhibition: Ticket Reminder",
     "Free tickets for the July blindfold exhibition are going fast. Reserve yours via the events page.",
     1, "2023-07-01 09:00:00"),
    (5, "Rating System Now Live",
     "We have launched an internal Elo rating system. Your rating updates after every official club game.",
     0, "2023-07-20 09:00:00"),
    (5, "Inter-Club Match: Support Needed",
     "Come cheer on our team at the September friendly against the University Chess Team. All are welcome.",
     1, "2023-09-10 09:00:00"),
    (5, "Grand Tournament: 28 of 32 Spots Filled",
     "Only 4 spots remain in the November Grand Tournament. Register before November 15th.",
     1, "2023-11-08 09:00:00"),
    (5, "Club of the Year Award",
     "We have been nominated for Club of the Year by the City Community Council. Voting is open to the public!",
     1, "2023-11-20 09:00:00"),
    # Film & Cinema Club (6) — batches 2 & 3
    (6, "Welcome to Film & Cinema Club!",
     "Our first screening is set for July — a Kurosawa double feature. Check the events page to RSVP.",
     1, "2023-05-20 09:30:00"),
    (6, "Member Discount at Cinéclub Theatre",
     "All members now receive 20% off regular ticket prices at Cinéclub Theatre. Show your member card.",
     0, "2023-06-10 10:00:00"),
    (6, "Film of the Month: \"Parasite\"",
     "July's film of the month is Bong Joon-ho's \"Parasite\". Discussion thread now open in the forum.",
     0, "2023-07-01 09:00:00"),
    (6, "New Wave Night: Background Reading",
     "Brush up before our New Wave screening with this short article on the movement's origins. Link in portal.",
     0, "2023-08-05 09:00:00"),
    (6, "Short Film Submissions Open",
     "Submit your short film (under 10 min, any genre) for October's showcase. Deadline: October 5th.",
     0, "2023-09-15 09:00:00"),
    (6, "Halloween Marathon: Costume Prize",
     "Best costume at the Halloween horror marathon wins a 3-month Cinéclub Theatre membership. Dress up!",
     1, "2023-10-10 09:00:00"),
    (6, "Film Club Zine Coming Soon",
     "We are producing a quarterly film zine written by members. Pitch your review or essay by November 15th.",
     0, "2023-11-01 09:00:00"),
    (6, "Best Films of 2023: Nominations Open",
     "Submit your nominations for best film of 2023 before our December session. All genres counted.",
     0, "2023-12-01 09:00:00"),
    # Sustainable Living Network (7) — batches 2 & 3
    (7, "Launch Announcement",
     "The Sustainable Living Network is officially open! Join us and help build a greener community.",
     1, "2023-05-25 10:30:00"),
    (7, "Compost Drop-Off Point Added",
     "We now have a compost drop-off point at the Community Kitchen every Saturday morning.",
     0, "2023-06-20 09:00:00"),
    (7, "Eco Challenge: July Plastic-Free Month",
     "We are running a plastic-free month challenge in July. Sign up via the members portal.",
     1, "2023-06-28 08:00:00"),
    (7, "Plastic-Free July: Results",
     "Incredible effort from all members this July! Together we avoided an estimated 2,400 single-use plastic items.",
     1, "2023-08-01 09:00:00"),
    (7, "Solar Session: Register Now",
     "The September solar energy info session has limited seating. Register via the events page by September 10th.",
     1, "2023-09-05 09:00:00"),
    (7, "Community Garden Plot Available",
     "One raised bed is available in the community garden for a member to adopt. Message Farid to claim it.",
     0, "2023-09-20 09:00:00"),
    (7, "Fashion Swap: Donation Guidelines",
     "Items must be clean, wearable, and free of major damage. Max 5 items per person for the October swap.",
     0, "2023-10-10 09:00:00"),
    (7, "2024 Initiative Proposals Open",
     "Submit your ideas for 2024 club initiatives via the portal. Top 3 voted proposals will be funded.",
     0, "2023-11-25 09:00:00"),
    # Creative Writing Workshop (8) — batches 2 & 3
    (8, "Welcome Writers!",
     "We are thrilled to open our doors. First session is a flash fiction sprint — no experience needed.",
     1, "2023-06-01 11:30:00"),
    (8, "Literary Magazine Call for Submissions",
     "Our first annual literary magazine is accepting submissions until August 31st. Members only.",
     0, "2023-07-15 10:00:00"),
    (8, "World Building Resources Shared",
     "A pack of world building templates, maps, and worksheets used in the August masterclass is now in the portal.",
     0, "2023-08-25 09:00:00"),
    (8, "Guest Writer Confirmed for October",
     "Award-winning essayist Nadia Farouq will join our October non-fiction evening. A real treat!",
     1, "2023-09-25 09:00:00"),
    (8, "NaNoWriMo Word Count Tracker",
     "We have set up a shared spreadsheet to track everyone's NaNoWriMo word counts. Link in the portal.",
     0, "2023-11-03 09:00:00"),
    (8, "Literary Magazine: Final Submissions",
     "Last call for literary magazine submissions. Deadline extended to November 15th. Poetry, prose, and essays.",
     0, "2023-11-08 09:00:00"),
    (8, "January Workshop: Plotting & Structure",
     "Our first 2024 session will be a deep dive into plotting methods — snowflake, three-act, and beat sheet.",
     0, "2023-12-10 09:00:00"),
    # Robotics & Makers Club (9) — batches 2 & 3
    (9, "Makerspace Now Open 7 Days a Week",
     "Members can now access the makerspace any day of the week between 8am and 10pm.",
     0, "2023-06-15 08:00:00"),
    (9, "Grant Funding Secured!",
     "We have secured a €5,000 grant to purchase new equipment. Vote on priorities in the members forum.",
     0, "2023-07-10 09:00:00"),
    (9, "Robot Sumo: Register Your Team",
     "Team registration for the August Robot Sumo Competition is now open. Max 12 teams.",
     1, "2023-07-20 10:00:00"),
    (9, "New Equipment Arrives This Week",
     "Our grant-funded equipment is arriving: 2 new 3D printers, a laser cutter, and a CNC router. Book time in the portal.",
     0, "2023-08-01 09:00:00"),
    (9, "Sumo Competition Teams Finalised",
     "All 12 teams for the Robot Sumo Competition are confirmed. Brackets posted on the notice board.",
     0, "2023-08-05 09:00:00"),
    (9, "Maker Showcase: Public Registration Open",
     "Members of the public can register to attend our October Open Maker Showcase. Spread the word!",
     1, "2023-09-25 09:00:00"),
    (9, "Safety Reminder: Laser Cutter Rules",
     "Please re-read the laser cutter safety guidelines before your first session. No exceptions.",
     0, "2023-10-05 09:00:00"),
    (9, "End of Year Challenge: Prize Announced",
     "The winning team of the December build challenge wins a €200 component voucher. May the best build win!",
     0, "2023-11-28 09:00:00"),
    # Yoga & Mindfulness Collective (10) — batches 2 & 3
    (10, "Welcome to the Collective",
     "We are so happy you are here. Our first outdoor session will take place on July 16th at sunrise.",
     1, "2023-06-15 09:30:00"),
    (10, "New: Monthly Newsletter",
     "Starting in August we will send a monthly wellness newsletter to all members. Look out for it!",
     0, "2023-07-20 09:00:00"),
    (10, "Partner Yoga: No Partner Needed",
     "A reminder that you do not need to bring a partner to the August partner yoga workshop — we pair everyone up!",
     1, "2023-08-08 09:00:00"),
    (10, "Autumn Schedule Released",
     "The full September to November class schedule is now available in the members portal.",
     0, "2023-08-28 09:00:00"),
    (10, "Equinox Practice: Weather Backup Plan",
     "If it rains on September 23rd, the equinox practice will move indoors to the Wellness Studio.",
     0, "2023-09-20 09:00:00"),
    (10, "Guided Meditation Audio Library",
     "Members can now access 20 guided meditation recordings (5 to 30 minutes) via the portal. Perfect for home practice.",
     0, "2023-10-15 09:00:00"),
    (10, "New Year Retreat: Last 3 Spots",
     "Only 3 spots remain for the December 30th New Year Intentions Retreat. Book now to avoid missing out.",
     1, "2023-12-20 09:00:00"),
]

# --------------------------------------------------------------------------- #
# Adhesions                                                                    #
# (club_id, user_id, status_int, created_at)                                  #
# status: 0=Pending, 1=Approved, 2=Rejected                                   #
# --------------------------------------------------------------------------- #
_STATUS = {0: "Pending", 1: "Approved", 2: "Rejected"}

ADHESIONS = [
    # Batch 1 — approved
    (1, 1,  1, "2023-01-20 09:00:00"), (1, 2,  1, "2023-01-21 09:30:00"),
    (1, 3,  1, "2023-01-22 10:00:00"), (1, 4,  1, "2023-01-25 11:00:00"),
    (1, 5,  1, "2023-02-01 08:00:00"),
    (2, 6,  1, "2023-01-25 10:00:00"), (2, 7,  1, "2023-01-26 10:30:00"),
    (2, 8,  1, "2023-01-27 11:00:00"), (2, 9,  1, "2023-02-01 09:00:00"),
    (2, 10, 1, "2023-02-05 09:30:00"),
    (3, 11, 1, "2023-02-01 11:00:00"), (3, 12, 1, "2023-02-02 11:30:00"),
    (3, 13, 1, "2023-02-03 09:00:00"), (3, 14, 1, "2023-02-10 09:30:00"),
    (3, 1,  1, "2023-02-15 10:00:00"),
    (4, 15, 1, "2023-02-15 08:00:00"), (4, 16, 1, "2023-02-16 08:30:00"),
    (4, 17, 1, "2023-02-20 09:00:00"), (4, 18, 1, "2023-03-01 08:30:00"),
    (4, 19, 1, "2023-03-05 09:00:00"),
    (5, 20, 1, "2023-03-01 09:00:00"), (5, 4,  1, "2023-03-02 09:30:00"),
    (5, 5,  1, "2023-03-05 10:00:00"), (5, 10, 1, "2023-03-10 11:00:00"),
    (5, 14, 1, "2023-03-15 09:00:00"),
    # Batch 1 — pending
    (1, 16, 0, "2023-05-01 10:00:00"), (1, 17, 0, "2023-05-03 11:00:00"),
    (2, 18, 0, "2023-05-02 09:00:00"), (3, 19, 0, "2023-05-04 10:30:00"),
    (4, 11, 0, "2023-05-06 08:00:00"), (5, 7,  0, "2023-05-07 09:00:00"),
    # Batch 1 — rejected
    (1, 20, 2, "2023-02-10 10:00:00"), (2, 14, 2, "2023-02-20 11:00:00"),
    (4, 3,  2, "2023-03-10 09:00:00"),
    # Batch 2 — approved
    (6, 21, 1, "2023-05-20 09:00:00"), (6, 22, 1, "2023-05-21 09:30:00"),
    (6, 23, 1, "2023-05-22 10:00:00"), (6, 24, 1, "2023-05-25 11:00:00"),
    (6, 25, 1, "2023-06-01 08:00:00"),
    (7, 26, 1, "2023-05-25 10:00:00"), (7, 27, 1, "2023-05-26 10:30:00"),
    (7, 28, 1, "2023-05-27 11:00:00"), (7, 29, 1, "2023-06-01 09:00:00"),
    (7, 30, 1, "2023-06-05 09:30:00"),
    (8, 31, 1, "2023-06-01 11:00:00"), (8, 32, 1, "2023-06-02 11:30:00"),
    (8, 33, 1, "2023-06-03 09:00:00"), (8, 34, 1, "2023-06-10 09:30:00"),
    (8, 21, 1, "2023-06-15 10:00:00"),
    (9, 35, 1, "2023-06-10 08:00:00"), (9, 36, 1, "2023-06-11 08:30:00"),
    (9, 37, 1, "2023-06-12 09:00:00"), (9, 38, 1, "2023-06-15 08:30:00"),
    (9, 39, 1, "2023-06-18 09:00:00"),
    (10, 40, 1, "2023-06-15 09:00:00"), (10, 24, 1, "2023-06-16 09:30:00"),
    (10, 25, 1, "2023-06-17 10:00:00"), (10, 30, 1, "2023-06-20 11:00:00"),
    (10, 34, 1, "2023-06-22 09:00:00"),
    # Batch 2 — pending
    (6, 38, 0, "2023-07-01 10:00:00"), (7, 33, 0, "2023-07-03 11:00:00"),
    (8, 39, 0, "2023-07-05 09:00:00"), (9, 22, 0, "2023-07-07 10:30:00"),
    (10, 28, 0, "2023-07-09 08:00:00"), (6, 40, 0, "2023-07-11 09:00:00"),
    # Batch 2 — rejected
    (7, 35, 2, "2023-06-15 10:00:00"), (8, 26, 2, "2023-06-22 11:00:00"),
    (9, 31, 2, "2023-07-01 09:00:00"),
]

# --------------------------------------------------------------------------- #
# FAQs — platform-level help texts (no equivalent in SQL schema)              #
# --------------------------------------------------------------------------- #
FAQS = [
    ("Comment rejoindre un club ?",
     "Rendez-vous sur la page Clubs, choisissez un club, puis cliquez sur le bouton Rejoindre pour envoyer une demande d'adhesion.",
     "clubs"),
    ("Comment creer un nouveau club ?",
     "Depuis votre tableau de bord, cliquez sur Creer un club, renseignez le nom, la description et les documents, puis soumettez la demande a l'administration.",
     "clubs"),
    ("Comment payer la cotisation ?",
     "La cotisation se paie en ligne par carte bancaire depuis votre profil, rubrique Cotisations. Un recu vous est envoye par email.",
     "paiement"),
    ("Quel est le montant de la cotisation ?",
     "La cotisation standard est de 50 euros par semestre. Certains clubs peuvent appliquer un montant different precise sur leur page.",
     "paiement"),
    ("Comment quitter un club ?",
     "Allez sur la page du club concerne et cliquez sur Quitter le club. Votre adhesion sera annulee immediatement.",
     "clubs"),
    ("Comment s'inscrire a un evenement ?",
     "Ouvrez la page de l'evenement et cliquez sur Participer. Les evenements prives sont reserves aux membres du club organisateur.",
     "evenements"),
    ("Qui peut publier une annonce ?",
     "Seuls les membres du bureau d'un club (president, vice-president, secretaire) peuvent publier des annonces pour ce club.",
     "annonces"),
    ("Quelle est la difference entre une annonce publique et privee ?",
     "Une annonce publique est visible par tous les utilisateurs ; une annonce privee n'est visible que par les membres du club.",
     "annonces"),
    ("Comment devenir membre du bureau d'un club ?",
     "Les postes du bureau sont attribues par le president du club. Contactez le president ou candidatez lors de l'assemblee generale.",
     "clubs"),
    ("Comment contacter le support ?",
     "Vous pouvez demander a parler a un humain directement dans le chat, ou ecrire a support@uniclubs.example.com.",
     "support"),
    ("Comment reinitialiser mon mot de passe ?",
     "Sur la page de connexion, cliquez sur Mot de passe oublie et suivez le lien envoye par email pour definir un nouveau mot de passe.",
     "compte"),
    ("Comment modifier mon profil ?",
     "Cliquez sur votre avatar en haut a droite, puis sur Mon profil, pour modifier votre nom, votre email et votre photo.",
     "compte"),
    ("Les documents d'un club sont-ils accessibles a tous ?",
     "Les documents officiels d'un club (statuts, reglement) sont accessibles a ses membres depuis la page du club.",
     "clubs"),
    ("Comment fonctionne la validation d'une adhesion ?",
     "Une demande d'adhesion a le statut En attente jusqu'a sa validation par le bureau du club, qui peut l'approuver ou la refuser.",
     "clubs"),
    ("Puis-je etre membre de plusieurs clubs ?",
     "Oui, vous pouvez rejoindre autant de clubs que vous le souhaitez, chacun avec sa propre cotisation eventuelle.",
     "clubs"),
]


def seed() -> None:
    print("Dropping and recreating tables...")
    Base.metadata.drop_all(engine)
    Base.metadata.create_all(engine)

    db = SessionLocal()
    try:
        # --- Users ---
        user_map: dict[int, User] = {}
        for uid, email, first, last, created in USERS:
            obj = User(id=uid, email=email, first_name=first,
                       last_name=last, created_at=_dt(created))
            db.add(obj)
            user_map[uid] = obj
        db.flush()

        # --- Clubs ---
        for cid, name, desc, docs, created in CLUBS:
            db.add(Club(id=cid, name=name, description=desc,
                        documents=docs, created_at=_dt(created)))
        db.flush()

        # --- Members ---
        for mid, (club_id, user_id, post_int, created) in enumerate(MEMBERS, start=1):
            db.add(Member(id=mid, club_id=club_id, user_id=user_id,
                          post_in_club=_POST[post_int], created_at=_dt(created)))
        db.flush()

        # --- Events (with participants) ---
        # Build a lookup: event_id -> list of user_ids
        from collections import defaultdict
        participants_by_event: dict[int, list[int]] = defaultdict(list)
        for ev_id, u_id in EVENT_PARTICIPANTS:
            participants_by_event[ev_id].append(u_id)

        for eid, club_id, title, desc, is_pub_int, loc, start, tags_csv, created in EVENTS:
            ev = Event(
                id=eid, club_id=club_id, title=title, description=desc,
                is_public=bool(is_pub_int), location=loc,
                start_date=_dt(start),
                tags=[t for t in tags_csv.split(",") if t],
                created_at=_dt(created),
            )
            for u_id in participants_by_event.get(eid, []):
                if u_id in user_map:
                    ev.participants.append(user_map[u_id])
            db.add(ev)
        db.flush()

        # --- Announcements ---
        for ann_id, (club_id, title, content, is_pub_int, created) in enumerate(ANNOUNCEMENTS, start=1):
            db.add(Announcement(id=ann_id, club_id=club_id, title=title,
                                content=content, is_public=bool(is_pub_int),
                                created_at=_dt(created)))
        db.flush()

        # --- Adhesions ---
        for adh_id, (club_id, user_id, status_int, created) in enumerate(ADHESIONS, start=1):
            db.add(Adhesion(id=adh_id, club_id=club_id, user_id=user_id,
                            status=_STATUS[status_int], created_at=_dt(created)))
        db.flush()

        # --- FAQs ---
        for q, a, cat in FAQS:
            db.add(FAQ(question=q, answer=a, category=cat))

        db.commit()
        print(
            f"Inserted: {len(USERS)} users, {len(CLUBS)} clubs, "
            f"{len(MEMBERS)} members, {len(EVENTS)} events, "
            f"{len(EVENT_PARTICIPANTS)} participant links, "
            f"{len(ANNOUNCEMENTS)} announcements, {len(ADHESIONS)} adhesions, "
            f"{len(FAQS)} FAQs."
        )

        # --- Vector-store indexing ---
        print("Indexing FAQs into ChromaDB...")
        faq_indexing_service.index_all(db)
        print("Indexing domain entities (clubs/events/announcements) into ChromaDB...")
        n = knowledge_service.index_all(db)
        print(f"Domain documents indexed: {n}")
    finally:
        db.close()

    print("Seed complete.")


if __name__ == "__main__":
    seed()
