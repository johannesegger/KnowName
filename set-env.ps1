$env:DOTNET_ROOT=Split-Path -Parent (Get-Command dotnet).Path
$env:SISDB_CONNECTION_STRING="Server=localhost;Port=8081;Database=sis2;User=root;Password=1234"
$env:TEACHER_IMAGE_DIR="$PSScriptRoot\test\images\teachers"
$env:STUDENT_IMAGE_DIR="$PSScriptRoot\test\images\students"
