function exportToPDF() {
    const { jsPDF } = window.jspdf;
    const doc = new jsPDF();
    doc.autoTable({ html: '#studentsTable' });

    const lectureTitle = document.querySelector('h3:nth-child(2)').innerText.trim();
    const today = new Date();
    const formattedDate = `${today.getFullYear()}-${(today.getMonth() + 1).toString().padStart(2, '0')}-${today.getDate().toString().padStart(2, '0')}`;
    const fileName = `${lectureTitle}_${formattedDate}.pdf`;

    doc.save(fileName);
}

function exportToExcel() {
    const table = document.getElementById('studentsTable');
    const wb = XLSX.utils.table_to_book(table, { sheet: 'Sheet1' });
    const wbout = XLSX.write(wb, { bookType: 'xlsx', bookSST: true, type: 'binary' });

    const lectureTitle = document.querySelector('h3:nth-child(2)').innerText.trim();
    const today = new Date();
    const formattedDate = `${today.getFullYear()}-${(today.getMonth() + 1).toString().padStart(2, '0')}-${today.getDate().toString().padStart(2, '0')}`;
    const fileName = `${lectureTitle}_${formattedDate}.xlsx`;

    saveAs(new Blob([s2ab(wbout)], { type: 'application/octet-stream' }), fileName);
}

function s2ab(s) {
    const buf = new ArrayBuffer(s.length);
    const view = new Uint8Array(buf);
    for (let i = 0; i < s.length; i++) {
        view[i] = s.charCodeAt(i) & 0xFF;
    }
    return buf;
}


