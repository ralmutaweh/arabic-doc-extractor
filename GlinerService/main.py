from fastapi import FastAPI
from gliner import GLiNER
from pydantic import BaseModel

app = FastAPI()

model = GLiNER.from_pretrained("NAMAA-Space/gliner_arabic-v2.1")

class ExtractionRequest(BaseModel):
    text: str

ENTITIES = [
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
]

KEYWORDS = [
    "جريمة جنائية",
    "حادث مروري",
    "تحقيق جنائي",
    "اعتقال شخص",
    "تهريب مخدرات",
    "جريمة سرقة",
    "عملية احتيال",
    "قضية فساد",
    "عمل إرهابي",
    "تهريب بضائع",
    "قضية قانونية",
    "جلسة محكمة",
    "حكم قضائي",
    "غرامة مالية",
    "عقوبة سجن",
]

COUNTRIES = [
    "مملكة البحرين",
    "المملكة العربية السعودية",
    "الإمارات العربية المتحدة",
    "دولة الكويت",
    "سلطنة عُمان",
    "دولة قطر",
    "جمهورية مصر العربية",
    "الأردن",
    "العراق",
    "اليمن",
    "سوريا",
    "لبنان",
    "ليبيا",
    "تونس",
    "الجزائر",
    "المغرب",
    "السودان",
    "الصومال",
    "موريتانيا",
    "جيبوتي",
    "جزر القمر",
    "فلسطين",
    "المملكة المتحدة",
    "الولايات المتحدة الأمريكية",
    "فرنسا",
    "ألمانيا",
    "الهند",
    "باكستان",
    "بنغلاديش",
    "الفلبين",
    "إندونيسيا",
    "الصين",
    "تركيا",
    # Informal variants — commonly used in documents
    "البحرين",
    "السعودية",
    "الإمارات",
    "الكويت",
    "عُمان",
    "قطر",
    "مصر",
    "العراق",
    "الأردن",
    "اليمن",
    "تونس",
    "الجزائر",
    "المغرب",
    "السودان",
    "لبنان",
    "سوريا",
    "ليبيا",
    "فلسطين",
    "تركيا",
    "الهند",
    "باكستان",
    "الصين",
    "فرنسا",
    "ألمانيا",
]

@app.post("/extract")
async def extract(request: ExtractionRequest):
    semantic_labels = ["اسم الشخص"]

    semantic_entities_extracted = model.predict_entities(request.text, semantic_labels)
    entity_matches_extracted = model.predict_entities(request.text, ENTITIES, threshold=0.4)
    country_matches_extracted = model.predict_entities(request.text, COUNTRIES, threshold=0.35)
    keyword_matches_extracted = model.predict_entities(request.text, KEYWORDS, threshold=0.5)
    result = {
        "names": None,
        "countries": None,
        "entities": None,
        "keywords": None,
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
                result['countries'] = []
            if country["text"] not in result["countries"]:
             result["countries"].append(country["text"])

    for entity in keyword_matches_extracted:
        if entity["text"] in KEYWORDS:
            if result["keywords"] is None:
                result["keywords"] = []
            result["keywords"].append(entity["text"])

    return result