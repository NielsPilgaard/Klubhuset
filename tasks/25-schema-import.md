Schools might be using other schema software today. We need to allow import of schema from other sources. We're not familiar with the format, so we need to be smart about this. 

We can use:
- OCR (PaddleOCR for example)
- AI (like we do in task 21)

OCR + AI should be able to see and categorise courses, rooms, teacher names from existing schemas, either images, pdfs or something like that. Plan it out.
If required, we can have this in a fastapi that we host in aspire & docker compose, I don't know how good OCR libs are in dotnet