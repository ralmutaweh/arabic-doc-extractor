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

# Placeholder — replace with full keyword list when air gap system is ready
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

@app.post("/extract")
async def extract(request: ExtractionRequest):
    semantic_labels = ["اسم الشخص", "دولة"]

    semantic_entities_extracted = model.predict_entities(request.text, semantic_labels)
    entity_matches_extracted = model.predict_entities(request.text, ENTITIES, threshold=0.4)
    keyword_matches_extracted = model.predict_entities(request.text, KEYWORDS, threshold=0.5)

    result = {
        "names": None,
        "countries": None,
        "entities": None,
        "keywords": None,
        "description": None,
    }

    for entity in semantic_entities_extracted:
        if entity["label"] == "اسم الشخص":
            if result["names"] is None:
                result["names"] = []
            result["names"].append(entity["text"])
        elif entity["label"] == "دولة":
            if result["countries"] is None:
                result["countries"] = []
            result["countries"].append(entity["text"])

    for entity in entity_matches_extracted:
        if result["entities"] is None:
            result["entities"] = []
        result["entities"].append(entity["text"])

    for entity in keyword_matches_extracted:
        if result["keywords"] is None:
            result["keywords"] = []
        result["keywords"].append(entity["text"])

    return result