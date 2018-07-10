# KnowName
Web-based game for learning student names given their profile picture.

## Run in Docker
```
docker run -d -p 80:8085 -v /path/to/teacher/images:/images/teachers -v /path/to/student/images:images/students -e TEACHER_IMAGES_PATH=/images/teachers -e STUDENT_IMAGES_PATH=/images/students -e DB_CONNECTION_STRING=... johannesegger/know-name
```
