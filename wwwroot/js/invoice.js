document.addEventListener("DOMContentLoaded", () => {
  const fileInput = document.getElementById("fileInput");
  const uploadBtn = document.getElementById("uploadBtn");
  const openPdfBtn = document.getElementById("openPdfBtn");
  const resultsTableBody = document.querySelector("#resultsTable tbody");
  const pdfViewer = document.getElementById("pdfViewer");
  const errorBox = document.getElementById("errorBox");
  const clearBtn = document.getElementById("clearBtn");

//   const loaded = () => {
//     pdfViewer.src = "";
//   }

  let currentFile = null;
  let previewUrl = null;

  clearBtn.addEventListener("click", () => { 
    if(!pdfViewer.src) {
       errorBox.textContent = "Please choose a file first."; return; 
    }
    fileInput.value = "";
    currentFile = null;
    previewUrl = null;
    resultsTableBody.innerHTML = "";
    errorBox.textContent = "";
    pdfViewer.src = "";
  });



  fileInput.addEventListener("change", (e) => {
    currentFile = e.target.files[0] || null;
    resultsTableBody.innerHTML = "";
    errorBox.textContent = "";
    if (previewUrl) { URL.revokeObjectURL(previewUrl); previewUrl = null; }
    pdfViewer.src = "";
          if (!previewUrl) previewUrl = URL.createObjectURL(currentFile);
      pdfViewer.src = previewUrl + "#toolbar=1&navpanes=0";
  });

  openPdfBtn.addEventListener("click", () => {
    if (!currentFile) { errorBox.textContent = "Please choose a file first."; return; }
    if (!previewUrl) previewUrl = URL.createObjectURL(currentFile);
    window.open(previewUrl, "_blank", "noopener");
  });

  uploadBtn.addEventListener("click", async () => {
    if (!currentFile) { errorBox.textContent = "Please choose a file first."; return; }

    const formData = new FormData();
    formData.append("file", currentFile);

    try {
      errorBox.textContent = "";
      uploadBtn.textContent = "Analyzing...";
      uploadBtn.disabled = true;

      const res = await fetch("/api/analyze", {
        method: "POST",
        body: formData
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(`Analyze failed: ${res.status} ${res.statusText} — ${text}`);
      }

      const data = await res.json();
      renderResults(data);



    } catch (err) {
      console.error(err);
      errorBox.textContent = err.message || "Upload failed.";
    } finally {
      uploadBtn.textContent = "Upload & Analyze";
      uploadBtn.disabled = false;
    }
  });

  function renderResults(data) {
    resultsTableBody.innerHTML = "";
    Object.entries(data).forEach(([key, value], idx) => {
      const tr = document.createElement("tr");
      if (idx % 2) tr.style.background = "#fafafa";

      const tdKey = document.createElement("td");
      tdKey.style.padding = ".55rem";
      tdKey.style.borderBottom = "1px solid #f3f4f6";
      tdKey.style.whiteSpace = "nowrap";
      tdKey.textContent = key;

      const tdVal = document.createElement("td");
      tdVal.style.padding = ".55rem";
      tdVal.style.borderBottom = "1px solid #f3f4f6";
      tdVal.textContent = (typeof value === "object" && value !== null)
        ? (value.text ?? JSON.stringify(value))
        : String(value);

      tr.appendChild(tdKey);
      tr.appendChild(tdVal);
      resultsTableBody.appendChild(tr);
    });
  }
});
