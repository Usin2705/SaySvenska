# For more information, please refer to https://aka.ms/vscode-docker-python
FROM python:3.12.2

# Update package list and install dependencies
RUN apt-get update && \
    apt-get install -y \
        nano \
        ffmpeg \
        libsndfile1 \
        libsndfile1-dev

# Keeps Python from generating .pyc files in the container
# Turns off buffering for easier container logging
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1

# Change Docker language to deal with UTF-8
ENV LANG=C.UTF-8 LC_ALL=C.UTF-8

# Copy requirements.txt to the image
COPY requirements.txt .

# Install Python dependencies
RUN pip install --trusted-host pypi.python.org -r requirements.txt

# Install SoundFile Python library
RUN pip install soundfile

# Only copy relevant files and folders to /app folder
COPY ./models/ /app/models/
COPY ./utils /app/utils
COPY main.py /app/main.py

# Expose port 52705
EXPOSE 52705
ENV PORT 52705

# Set up some quick command
RUN echo "alias ll='ls -la'" >> ~/.bashrc

WORKDIR /app

# During debugging, this entry point will be overridden. For more information, please refer to https://aka.ms/vscode-docker-python-debug
#CMD ["gunicorn", "--bind", "0.0.0.0:5000", "main.main:app"]
RUN mkdir -p -v logs

CMD exec gunicorn --bind 0.0.0.0:$PORT main:app --workers 2 --threads 2 --access-logfile /app/logs/gunicorn-access.log --error-logfile /app/logs/gunicorn-error.log --capture-output --log-level debug --access-logformat "%(h)s %(l)s %(u)s %(t)s \"%(r)s\" %(s)s %(b)s \"%(f)s\" \"%(a)s\" %(L)s"
# gunicorn --bind :52705 main:app --workers 3 --threads 2
# 2 workers with 300mil model, 6 workers with 95mil models
# -timeout should be high the bigger the model, default is 30s
#ENTRYPOINT ["python", "main.py"]