FROM python:3.10-slim

WORKDIR /app

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY . .

ENV EMBEDDING_SERVICE_HOST=0.0.0.0
ENV EMBEDDING_SERVICE_PORT=5001

EXPOSE 5001

CMD sh -c "uvicorn embedding_service:app --host ${EMBEDDING_SERVICE_HOST} --port ${EMBEDDING_SERVICE_PORT}"