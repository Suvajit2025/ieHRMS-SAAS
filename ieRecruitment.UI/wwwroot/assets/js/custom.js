  // File validation
    document.getElementById('recruitmentForm').addEventListener('submit', function (e) {
      const cv = document.getElementById('cvUpload');
      const photo = document.getElementById('photoUpload');

      // Validate CV file extension
      const validCVTypes = ['application/pdf', 
                            'application/msword', 
                            'application/vnd.openxmlformats-officedocument.wordprocessingml.document'];
      if (cv.files.length === 0 || !validCVTypes.includes(cv.files[0].type)) {
        alert("Please upload a valid CV file (.pdf, .doc, .docx)");
        cv.focus();
        e.preventDefault();
        return;
      }

      // Validate photo file extension and size
      const validImageTypes = ['image/jpeg', 'image/png'];
      const photoFile = photo.files[0];

      if (!validImageTypes.includes(photoFile.type)) {
        alert("Please upload a valid photo (.jpg, .jpeg, .png)");
        photo.focus();
        e.preventDefault();
        return;
      }

      if (photoFile.size > 2 * 1024 * 1024) {
        alert("Photo size must be under 2MB.");
        photo.focus();
        e.preventDefault();
        return;
      }
    });