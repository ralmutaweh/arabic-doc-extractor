from fastapi import FastAPI
from gliner import GLiNER
from pydantic import BaseModel

app = FastAPI()

model = GLiNER.from_pretrained("NAMAA-Space/gliner_arabic-v2.1")

class ExtractionRequest(BaseModel): # BaseModel is a data schema to define the shape of the incoming request
    text: str

# Endpoint
@app.post("/extract")
async def extract(request: ExtractionRequest):
    
    labels = [
    "اسم الشخص",
    "الجنس",
    "العمر",
    "جنسية الشخص",
    "الرقم الشخصي",
    "تاريخ الميلاد",
    "رقم هاتف الشخص للتواصل",
    "رقم الفاكس",
    "عنوان البريد الإلكتروني",
    "الموقع",
    "العنوان",
    "اسم الشركة",
    "السجل التجاري",
    "المنظمة",
    "القسم",
    "المسمى الوظيفي",
    "تاريخ الإصدار",
    "المصدر",
    "رقم رخصة القيادة",
    "رقم اللوحة",
    "وقت الحادث",
    "تاريخ الحادث"
]

    entities = model.predict_entities(request.text, labels)

    result = {label: None for label in labels} # Built the dict with fields set to null

    for entity in entities:
        key = entity["label"]
        text = entity["text"]

        if result[key] is None:
            result[key] = [text]
        else:
            result[key].append(text)

    return result