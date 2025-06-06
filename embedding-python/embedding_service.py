from fastapi import FastAPI, Request
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
from typing import List
import uvicorn

app = FastAPI()
model = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")

class EmbedRequest(BaseModel):
    texts: List[str]

class EmbedResponse(BaseModel):
    embeddings: List[List[float]]

@app.post("/embed", response_model=EmbedResponse)
async def embed(req: EmbedRequest):
    vectors = model.encode(req.texts, convert_to_numpy=True).tolist()
    return {"embeddings": vectors}

if __name__ == "__main__":
    uvicorn.run(app, host="0.0.0.0", port=5001) 