"""Seed the UniClubs database with a large, realistic (French) dataset, then
(re)index every entity into the ChromaDB vector database.

This drops and recreates the tables, so it is meant for local development.

Run:
    cd uniclubs-chatbot-python
    $env:PYTHONPATH="."; python scripts/seed.py
"""
from datetime import datetime, timedelta, timezone

from app.db import Base, SessionLocal, engine
from app.models import Adhesion, Announcement, Club, Event, FAQ, Member, User
from app.services.faq_indexing_service import faq_indexing_service
from app.services.knowledge_service import knowledge_service

BASE = datetime(2024, 1, 10, 8, 0, tzinfo=timezone.utc)


def _dt(days: int = 0, hours: int = 0) -> datetime:
    return BASE + timedelta(days=days, hours=hours)


# --------------------------------------------------------------------------- #
# Users pool                                                                   #
# --------------------------------------------------------------------------- #
FIRST_NAMES = [
    "Alice", "Pierre", "Sophie", "Luc", "Marie", "Thomas", "Emma", "Hugo",
    "Lea", "Nathan", "Camille", "Louis", "Chloe", "Jules", "Manon", "Adam",
    "Ines", "Gabriel", "Sarah", "Raphael", "Julie", "Noah", "Laura", "Enzo",
    "Clara", "Ethan", "Lucie", "Tom", "Anna", "Mathis", "Eva", "Theo",
    "Jade", "Paul", "Lina", "Arthur", "Zoe", "Maxime", "Rose", "Antoine",
    "Nina", "Victor", "Lola", "Sacha", "Mila", "Oscar", "Romane", "Liam",
    "Alix", "Yanis",
]
LAST_NAMES = [
    "Martin", "Dubois", "Bernard", "Petit", "Rousseau", "Laurent", "Simon",
    "Michel", "Garcia", "David", "Bertrand", "Moreau", "Lefebvre", "Roux",
    "Fournier", "Girard", "Bonnet", "Dupont", "Lambert", "Fontaine",
    "Robert", "Richard", "Durand", "Leroy", "Morel", "Mercier", "Blanc",
    "Guerin", "Boyer", "Chevalier", "Francois", "Legrand", "Gauthier",
    "Garnier", "Faure", "Rousset", "Andre", "Lemaire", "Colin", "Vincent",
    "Henry", "Masson", "Marchand", "Duval", "Denis", "Dumont", "Marie",
    "Noel", "Meyer", "Perrin",
]

USERS = [
    {
        "id": 100 + i,
        "email": f"{FIRST_NAMES[i].lower()}.{LAST_NAMES[i].lower()}{i}@example.com",
        "first": FIRST_NAMES[i],
        "last": LAST_NAMES[i],
        "created": _dt(days=i % 20),
    }
    for i in range(50)
]

ROLES = ["President", "VicePresident", "Secretary", "Treasurer", "HeadOfDepartment", "Member", "Member", "Member"]

# --------------------------------------------------------------------------- #
# Clubs                                                                        #
# --------------------------------------------------------------------------- #
# Each club: name, description, documents, member user-id offsets, events, announcements.
CLUBS = [
    {
        "id": 1,
        "name": "Club Developpement Logiciel",
        "description": (
            "Un club dedie aux passionnes de developpement logiciel, de l'apprentissage des "
            "nouvelles technologies et du partage des bonnes pratiques de programmation."
        ),
        "documents": [
            "https://storage.example.com/clubs/dev-club/statuts.pdf",
            "https://storage.example.com/clubs/dev-club/reglement-interieur.pdf",
            "https://storage.example.com/clubs/dev-club/charte-membres.pdf",
        ],
        "members": [0, 1, 2, 3, 4, 5, 6],
        "events": [
            ("Workshop Git & GitHub Avance",
             "Atelier pratique sur Git/GitHub : rebasing, cherry-picking, resolution de conflits, Git hooks et GitHub Actions.",
             True, "Salle informatique A-301", _dt(days=55, hours=6), ["git", "github", "devops", "workshop"], [0, 1, 5, 6]),
            ("Coding Dojo : Clean Code & SOLID",
             "Session de coding dojo sur le Clean Code et les principes SOLID, en pair programming sur des katas de refactoring.",
             False, "Laboratoire B-104", _dt(days=62, hours=10), ["clean-code", "solid", "refactoring"], [0, 2, 4]),
            ("Hackathon IA & Developpement Durable",
             "Hackathon de 48h sur l'IA appliquee au developpement durable. Equipes de 3-5 personnes, mentors, prix de 1500 euros.",
             True, "Campus Principal - Batiment Innovation", _dt(days=72, hours=1), ["hackathon", "ai", "competition"], [0, 1, 2, 3, 5, 6]),
            ("Conference : Architecture Microservices",
             "Conference de Jean Dupont (architecte senior) sur les microservices, Kubernetes, Docker et la communication inter-services.",
             True, "Amphitheatre Central", _dt(days=55, hours=6), ["microservices", "kubernetes", "docker"], [0, 1, 3, 4]),
        ],
        "announcements": [
            ("Nouvelle session de formation .NET 9",
             "Nouvelle session de formation sur .NET 9 : nouvelles fonctionnalites, performances et meilleures pratiques. Inscriptions ouvertes.",
             True, _dt(days=22)),
            ("Reunion mensuelle - Fevrier 2024",
             "Reunion mensuelle le 15 fevrier a 18h00 en salle B205. Ordre du jour : bilan de janvier, hackathon de mars, nouveaux projets.",
             False, _dt(days=26)),
            ("Hackathon Mars 2024 - Inscriptions ouvertes",
             "Le hackathon annuel se tiendra du 22 au 24 mars. Theme : IA et developpement durable. Prix de 1500 euros pour l'equipe gagnante.",
             True, _dt(days=31)),
            ("Mise a jour des cotisations",
             "Rappel : les cotisations du second semestre doivent etre payees avant le 28 fevrier. Montant : 50 euros. Contactez le tresorier.",
             False, _dt(days=36)),
        ],
    },
]

# Themed clubs generated to scale the dataset up.
THEMES = [
    ("Club Robotique", "Conception et programmation de robots, competitions et ateliers d'electronique.",
     ["robotique", "arduino", "electronique"], "Atelier Robotique C-12"),
    ("Club Intelligence Artificielle", "Etude du machine learning, du deep learning et des LLM, avec projets pratiques.",
     ["ia", "machine-learning", "python"], "Salle Data B-201"),
    ("Club Cybersecurite", "Sensibilisation a la securite, CTF, audit et bonnes pratiques de defense.",
     ["securite", "ctf", "pentest"], "Lab Securite D-05"),
    ("Club Jeux Video", "Developpement de jeux, game jams et tournois entre membres.",
     ["gamedev", "unity", "tournoi"], "Salle Multimedia E-3"),
    ("Club Photographie", "Sorties photo, retouche et expositions des travaux des membres.",
     ["photo", "retouche", "expo"], "Studio Photo F-1"),
    ("Club Musique", "Repetitions, concerts et ateliers de composition assistee par ordinateur.",
     ["musique", "concert", "mao"], "Salle de Musique G-2"),
    ("Club Debat", "Joutes oratoires, debats d'actualite et entrainement a l'argumentation.",
     ["debat", "rhetorique", "actualite"], "Amphi B"),
    ("Club Entrepreneuriat", "Accompagnement de projets, pitchs et rencontres avec des startups.",
     ["startup", "pitch", "business"], "Espace Coworking H-1"),
    ("Club E-Sport", "Entrainements et competitions sur plusieurs jeux competitifs.",
     ["esport", "competition", "gaming"], "Arene E-Sport I-4"),
    ("Club Sciences", "Vulgarisation scientifique, experiences et conferences ouvertes a tous.",
     ["sciences", "experiences", "conference"], "Laboratoire Sciences J-7"),
    ("Club Arts Numeriques", "Creation graphique, modelisation 3D et art generatif.",
     ["3d", "design", "art-generatif"], "Atelier Creatif K-9"),
]

_event_titles = [
    ("Atelier d'introduction", "Atelier pour decouvrir les bases et rencontrer les membres du club."),
    ("Competition inter-clubs", "Competition amicale entre les membres et les clubs invites."),
    ("Conference invitee", "Conference animee par un intervenant professionnel du domaine."),
    ("Projet collaboratif", "Session de travail sur un projet collaboratif ouvert a tous les membres."),
]
_announce_titles = [
    ("Reunion de rentree", "Reunion de presentation du club et des activites du semestre. Tous les membres sont invites."),
    ("Appel a participation", "Nous recherchons des membres motives pour aider a organiser le prochain evenement."),
    ("Resultats de la derniere activite", "Merci a tous les participants. Retrouvez le compte rendu et les photos sur l'espace du club."),
    ("Rappel cotisation", "La cotisation semestrielle de 50 euros doit etre reglee avant la fin du mois aupres du tresorier."),
]

club_id = 2
user_cursor = 7  # club 1 used users 0..6
for ti, (name, desc, tags, location) in enumerate(THEMES):
    n_members = 6 + (ti % 3)  # 6, 7 or 8 members
    member_offsets = [(user_cursor + j) % 50 for j in range(n_members)]
    user_cursor = (user_cursor + n_members) % 50

    events = []
    for ei, (etitle, edesc) in enumerate(_event_titles):
        events.append((
            f"{etitle} - {name.replace('Club ', '')}",
            f"{edesc} Theme : {tags[0]}.",
            ei % 2 == 0,
            location,
            _dt(days=50 + club_id + ei * 5, hours=ei * 2),
            tags,
            member_offsets[: 3 + ei],
        ))

    announcements = []
    for ai, (atitle, acontent) in enumerate(_announce_titles):
        announcements.append((atitle, acontent, ai % 2 == 0, _dt(days=20 + club_id + ai * 3)))

    CLUBS.append({
        "id": club_id,
        "name": name,
        "description": desc,
        "documents": [
            f"https://storage.example.com/clubs/{club_id}/statuts.pdf",
            f"https://storage.example.com/clubs/{club_id}/reglement.pdf",
        ],
        "members": member_offsets,
        "events": events,
        "announcements": announcements,
    })
    club_id += 1


# --------------------------------------------------------------------------- #
# FAQs (platform help, French)                                                 #
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
        # Users
        users = {}
        for u in USERS:
            obj = User(id=u["id"], email=u["email"], first_name=u["first"],
                       last_name=u["last"], created_at=u["created"])
            db.add(obj)
            users[u["id"]] = obj
        db.flush()

        user_ids = [u["id"] for u in USERS]
        event_id = 1
        ann_id = 1
        member_id = 1
        adh_id = 1

        for c in CLUBS:
            club = Club(id=c["id"], name=c["name"], description=c["description"],
                        documents=c["documents"], created_at=_dt(days=c["id"]))
            db.add(club)
            db.flush()

            club_member_user_ids = []
            for j, off in enumerate(c["members"]):
                uid = user_ids[off]
                club_member_user_ids.append(uid)
                db.add(Member(id=member_id, club_id=club.id, user_id=uid,
                              post_in_club=ROLES[min(j, len(ROLES) - 1)],
                              created_at=_dt(days=c["id"], hours=j)))
                member_id += 1
                # Adhesion record for the leaders.
                if j < 3:
                    db.add(Adhesion(id=adh_id, club_id=club.id, user_id=uid,
                                    status="Approved", created_at=_dt(days=c["id"], hours=j)))
                    adh_id += 1

            for (etitle, edesc, epub, eloc, estart, etags, eparts) in c["events"]:
                ev = Event(id=event_id, club_id=club.id, title=etitle, description=edesc,
                           is_public=epub, location=eloc, start_date=estart, tags=etags,
                           created_at=_dt(days=c["id"]))
                # attach participants
                for off in eparts:
                    uid = user_ids[off]
                    if uid in users:
                        ev.participants.append(users[uid])
                db.add(ev)
                event_id += 1

            for (atitle, acontent, apub, acreated) in c["announcements"]:
                db.add(Announcement(id=ann_id, club_id=club.id, title=atitle,
                                    content=acontent, is_public=apub, created_at=acreated))
                ann_id += 1

        # FAQs
        for q, a, cat in FAQS:
            db.add(FAQ(question=q, answer=a, category=cat))

        db.commit()
        print(f"Inserted: {len(USERS)} users, {len(CLUBS)} clubs, "
              f"{member_id - 1} members, {event_id - 1} events, "
              f"{ann_id - 1} announcements, {adh_id - 1} adhesions, {len(FAQS)} FAQs.")

        # Index into the vector database
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
