from fastapi import FastAPI
from gliner import GLiNER
from pydantic import BaseModel

app = FastAPI()

model = GLiNER.from_pretrained("NAMAA-Space/gliner_arabic-v2.1")

class ExtractionRequest(BaseModel):
    text: str

# ── Entity Lists ──────────────────────────────────────────────────────────────
# HOW TO ADD ENTITIES:
# This list uses span matching — GLiNER looks for exact or near-exact text spans
# from this list inside the document. Add the exact Arabic name as it appears
# in documents. Threshold is set to 0.4 (40% confidence minimum).
# Example: to add a new ministry, just append its exact Arabic name to the list.

ENTITIES = [
    # Bahraini Ministries
    "وزارة الداخلية",
    "وزارة الخارجية",
    "وزارة المالية",
    "وزارة العدل والشؤون الإسلامية والأوقاف",
    "وزارة التربية والتعليم",
    "وزارة الصحة",
    "وزارة الأشغال وشؤون البلديات والتخطيط العمراني",
    "وزارة الصناعة والتجارة والسياحة",
    "وزارة التنمية الاجتماعية",
    "وزارة شؤون مجلس الوزراء",
    "وزارة شؤون مجلسي الشورى والنواب",
    "وزارة الإعلام",
    "وزارة النفط والبيئة",
    "وزارة الموارد البحرية والزراعة",
    "وزارة الدفاع",
    "وزارة شؤون الديوان الملكي",
    "وزارة المواصلات والاتصالات",
    "وزارة الإسكان",
    "وزارة العمل",
    "وزارة شؤون الشباب والرياضة",
    "وزارة السياحة",
    # Government Bodies & Authorities
    "هيئة المعلومات والحكومة الإلكترونية",
    "هيئة تنظيم الاتصالات",
    "هيئة السوق المالية",
    "جهاز المراقبة والتفتيش",
    "ديوان الخدمة المدنية",
    "الجهاز المركزي للمعلومات",
    "مجلس التنمية الاقتصادية",
    "بنك البحرين المركزي",
    "المجلس الأعلى للمرأة",
    "مجلس الشورى",
    "مجلس النواب",
    "النيابة العامة",
    "الجهاز القضائي",
]

# ── Countries List ────────────────────────────────────────────────────────────
# HOW TO ADD COUNTRIES:
# This list uses span matching with a lower threshold (0.35) since country names
# appear in many informal variants. Add both the formal and informal Arabic name
# for any country you want to detect. Always add all known variants.
# Example: both "مملكة البحرين" (formal) and "البحرين" (informal) are listed.

COUNTRIES = [
    # Bahrain — formal and all variants
    "مملكة البحرين",
    "البحرين",
    "دولة البحرين",
    "جزر البحرين",
    "Bahrain",
    # Gulf States — formal
    "المملكة العربية السعودية",
    "الإمارات العربية المتحدة",
    "دولة الكويت",
    "سلطنة عُمان",
    "دولة قطر",
    # Gulf States — informal
    "السعودية",
    "الإمارات",
    "الكويت",
    "عُمان",
    "قطر",
    # Arab World — formal
    "جمهورية مصر العربية",
    "الجمهورية العراقية",
    "الجمهورية اللبنانية",
    "الجمهورية العربية السورية",
    "الجمهورية التونسية",
    "الجمهورية الجزائرية",
    "المملكة المغربية",
    "جمهورية السودان",
    "الجمهورية اليمنية",
    "المملكة الأردنية الهاشمية",
    "الجماهيرية الليبية",
    "جمهورية الصومال",
    "الجمهورية الإسلامية الموريتانية",
    "جمهورية جيبوتي",
    "اتحاد جزر القمر",
    "دولة فلسطين",
    # Arab World — informal
    "مصر",
    "العراق",
    "لبنان",
    "سوريا",
    "تونس",
    "الجزائر",
    "المغرب",
    "السودان",
    "اليمن",
    "الأردن",
    "ليبيا",
    "الصومال",
    "موريتانيا",
    "جيبوتي",
    "جزر القمر",
    "فلسطين",
    # International
    "المملكة المتحدة",
    "الولايات المتحدة الأمريكية",
    "الولايات المتحدة",
    "أمريكا",
    "بريطانيا",
    "فرنسا",
    "ألمانيا",
    "الهند",
    "باكستان",
    "بنغلاديش",
    "الفلبين",
    "إندونيسيا",
    "الصين",
    "تركيا",
    "إيران",
    "روسيا",
    "اليابان",
    "كوريا الجنوبية",
    "إيطاليا",
    "إسبانيا",
    "كندا",
    "أستراليا",
]


@app.post("/extract")
async def extract(request: ExtractionRequest):
    # HOW EXTRACTION WORKS:
    #
    # NAMES — uses SEMANTIC matching via the label "اسم الشخص" (person name).
    # GLiNER understands the concept of a person name contextually, so it can
    # detect names it has never seen before. No predefined list needed.
    # To add more semantic labels (e.g. job titles), add to semantic_labels
    # and handle the new label in the loop below.
    #
    # ENTITIES & COUNTRIES — use SPAN matching against the lists above.
    # GLiNER looks for text spans that closely match items in the list.
    # threshold controls minimum confidence: lower = more matches but more noise,
    # higher = fewer matches but more precise. Current values are tuned for
    # Bahraini government documents after benchmarking.

    semantic_labels = ["اسم الشخص"]

    semantic_entities_extracted = model.predict_entities(request.text, semantic_labels)
    entity_matches_extracted = model.predict_entities(request.text, ENTITIES, threshold=0.4)
    country_matches_extracted = model.predict_entities(request.text, COUNTRIES, threshold=0.35)

    result = {
        "names": None,
        "countries": None,
        "entities": None,
    }

    for semantic_entity in semantic_entities_extracted:
        if semantic_entity["label"] == "اسم الشخص":
            if result["names"] is None:
                result["names"] = []
            result["names"].append(semantic_entity["text"])

    for entity in entity_matches_extracted:
        if entity["text"] in ENTITIES:
            if result["entities"] is None:
                result["entities"] = []
            result["entities"].append(entity["text"])

    for country in country_matches_extracted:
        if country["text"] in COUNTRIES:
            if result["countries"] is None:
                result["countries"] = []
            if country["text"] not in result["countries"]:
                result["countries"].append(country["text"])

    return result