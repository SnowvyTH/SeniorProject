/**
 * ฟังก์ชันสำหรับสร้าง pagination HTML ที่สามารถใช้ซ้ำได้
 * @param {number} totalPages - จำนวนหน้าทั้งหมด
 * @param {number} currentPageNum - หน้าปัจจุบัน
 * @param {string} targetElement - ID ของ element ที่จะแสดง pagination
 * @param {function|string} callbackFunction - ฟังก์ชันหรือชื่อฟังก์ชันที่จะเรียกเมื่อเปลี่ยนหน้า
 */
function generatePagination(totalPages, currentPageNum, targetElement, callbackFunction) {
    let paginationHtml = '';
    
    if (totalPages <= 1) {
        $(`#${targetElement}`).html('');
        $(`#${targetElement}`).parent().hide();
        return;
    }
    
    $(`#${targetElement}`).parent().show();
    
    // สร้าง unique ID สำหรับ pagination นี้
    const uniqueId = `pagination_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`;
    
    // เก็บ callback function ไว้ใน global scope
    if (typeof callbackFunction === 'function') {
        window[uniqueId] = callbackFunction;
    } else {
        window[uniqueId] = window[callbackFunction];
    }
    
    // Previous button
    paginationHtml += `
        <li class="page-item mx-1 ${currentPageNum <= 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="if(window['${uniqueId}']) window['${uniqueId}'](${currentPageNum - 1}); return false;">
                <i class="fas fa-chevron-left me-1"></i>ก่อนหน้า
            </a>
        </li>
    `;
    
    // Page numbers
    for (let i = 1; i <= totalPages; i++) {
        if (i <= 3 || i > totalPages - 3 || Math.abs(i - currentPageNum) <= 1) {
            paginationHtml += `
                <li class="page-item mx-1 ${i === currentPageNum ? 'active' : ''}">
                    <a class="page-link" href="#" onclick="if(window['${uniqueId}']) window['${uniqueId}'](${i}); return false;">${i}</a>
                </li>
            `;
        } else if (i === 4 && currentPageNum > 5) {
            paginationHtml += '<li class="page-item disabled"><span class="page-link">...</span></li>';
        } else if (i === totalPages - 3 && currentPageNum < totalPages - 4) {
            paginationHtml += '<li class="page-item disabled"><span class="page-link">...</span></li>';
        }
    }
    
    // Next button
    paginationHtml += `
        <li class="page-item mx-1 ${currentPageNum >= totalPages ? 'disabled' : ''}">
            <a class="page-link" href="#" onclick="if(window['${uniqueId}']) window['${uniqueId}'](${currentPageNum + 1}); return false;">
                ถัดไป<i class="fas fa-chevron-right ms-1"></i>
            </a>
        </li>
    `;
    
    $(`#${targetElement}`).html(paginationHtml);
}

/**
 * ฟังก์ชันสำหรับแบ่งข้อมูลเป็นหน้า ๆ (client-side pagination)
 * @param {Array} data - ข้อมูลทั้งหมด
 * @param {number} page - หน้าปัจจุบัน
 * @param {number} pageSize - จำนวนข้อมูลต่อหน้า
 * @returns {Object} - { items: ข้อมูลในหน้านี้, totalPages: จำนวนหน้าทั้งหมด, currentPage: หน้าปัจจุบัน }
 */
function paginateData(data, page, pageSize) {
    const totalPages = Math.ceil(data.length / pageSize);
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const items = data.slice(startIndex, endIndex);
    
    return {
        items: items,
        totalPages: totalPages,
        currentPage: page,
        totalItems: data.length
    };
}

/**
 * ฟังก์ชันสำหรับแสดงข้อมูลสถิติของ pagination
 * @param {number} currentPage - หน้าปัจจุบัน
 * @param {number} pageSize - จำนวนข้อมูลต่อหน้า
 * @param {number} totalItems - จำนวนข้อมูลทั้งหมด
 * @param {string} targetElement - ID ของ element ที่จะแสดงข้อมูลสถิติ
 */
function updatePaginationInfo(currentPage, pageSize, totalItems, targetElement) {
    const startItem = totalItems === 0 ? 0 : (currentPage - 1) * pageSize + 1;
    const endItem = Math.min(currentPage * pageSize, totalItems);
    
    const infoText = `แสดง ${startItem}-${endItem} จาก ${totalItems} รายการ`;
    $(`#${targetElement}`).text(infoText);
}
