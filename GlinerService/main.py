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
        "full_name",
        "gender",
        "age",
        "nationality",
        "national_id_number",
        "date_of_birth",
        "phone_number",
        "fax_number",
        "email_address",
        "location",
        "address",
        "company_name",
        "cr_number",
        "organisation",
        "department",
        "role_title",
        "document_issue_date",
        "source_informant",
        "driving_license_number",
        "car_plate_number",
        "time_of_incident",
        "date_of_incident"
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