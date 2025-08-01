class ImageCropper {
    constructor(options = {}) {
        this.options = {
            aspectRatio: 1, // 1:1 ratio for profile pictures
            width: 300,
            height: 300,
            quality: 0.8,
            ...options
        };
        
        this.canvas = null;
        this.ctx = null;
        this.image = null;
        this.isDragging = false;
        this.startX = 0;
        this.startY = 0;
        this.cropX = 0;
        this.cropY = 0;
        this.cropSize = 200;
    }

    init(imageElement, containerElement) {
        this.image = imageElement;
        this.container = containerElement;
        
        // Create canvas
        this.canvas = document.createElement('canvas');
        this.ctx = this.canvas.getContext('2d');
        
        // Set canvas size
        const containerRect = this.container.getBoundingClientRect();
        this.canvas.width = containerRect.width;
        this.canvas.height = containerRect.height;
        
        // Style canvas
        this.canvas.style.border = '1px solid #ddd';
        this.canvas.style.cursor = 'crosshair';
        
        // Clear container and add canvas
        this.container.innerHTML = '';
        this.container.appendChild(this.canvas);
        
        // Calculate image dimensions to fit container
        this.calculateImageDimensions();
        
        // Initialize crop area in center
        this.initializeCropArea();
        
        // Add event listeners
        this.addEventListeners();
        
        // Initial draw
        this.draw();
    }
    
    calculateImageDimensions() {
        const canvasWidth = this.canvas.width;
        const canvasHeight = this.canvas.height;
        const imageWidth = this.image.naturalWidth;
        const imageHeight = this.image.naturalHeight;
        
        // Calculate scale to fit image in canvas
        const scale = Math.min(canvasWidth / imageWidth, canvasHeight / imageHeight);
        
        this.scaledWidth = imageWidth * scale;
        this.scaledHeight = imageHeight * scale;
        this.offsetX = (canvasWidth - this.scaledWidth) / 2;
        this.offsetY = (canvasHeight - this.scaledHeight) / 2;
        this.scale = scale;
    }
    
    initializeCropArea() {
        // Set crop size to fit within scaled image
        const maxCropSize = Math.min(this.scaledWidth, this.scaledHeight) * 0.8;
        this.cropSize = Math.min(maxCropSize, 200);
        
        // Center crop area
        this.cropX = this.offsetX + (this.scaledWidth - this.cropSize) / 2;
        this.cropY = this.offsetY + (this.scaledHeight - this.cropSize) / 2;
    }
    
    addEventListeners() {
        this.canvas.addEventListener('mousedown', this.onMouseDown.bind(this));
        this.canvas.addEventListener('mousemove', this.onMouseMove.bind(this));
        this.canvas.addEventListener('mouseup', this.onMouseUp.bind(this));
        this.canvas.addEventListener('wheel', this.onWheel.bind(this));
    }
    
    onMouseDown(e) {
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        
        // Check if click is within crop area
        if (x >= this.cropX && x <= this.cropX + this.cropSize &&
            y >= this.cropY && y <= this.cropY + this.cropSize) {
            this.isDragging = true;
            this.startX = x - this.cropX;
            this.startY = y - this.cropY;
            this.canvas.style.cursor = 'move';
        }
    }
    
    onMouseMove(e) {
        if (!this.isDragging) return;
        
        const rect = this.canvas.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;
        
        // Calculate new crop position
        let newCropX = x - this.startX;
        let newCropY = y - this.startY;
        
        // Constrain crop area within image bounds
        newCropX = Math.max(this.offsetX, Math.min(newCropX, this.offsetX + this.scaledWidth - this.cropSize));
        newCropY = Math.max(this.offsetY, Math.min(newCropY, this.offsetY + this.scaledHeight - this.cropSize));
        
        this.cropX = newCropX;
        this.cropY = newCropY;
        
        this.draw();
    }
    
    onMouseUp() {
        this.isDragging = false;
        this.canvas.style.cursor = 'crosshair';
    }
    
    onWheel(e) {
        e.preventDefault();
        
        const delta = e.deltaY > 0 ? -10 : 10;
        const newSize = this.cropSize + delta;
        const minSize = 50;
        const maxSize = Math.min(this.scaledWidth, this.scaledHeight);
        
        if (newSize >= minSize && newSize <= maxSize) {
            // Adjust crop position to keep it centered on the same point
            const centerX = this.cropX + this.cropSize / 2;
            const centerY = this.cropY + this.cropSize / 2;
            
            this.cropSize = newSize;
            this.cropX = centerX - this.cropSize / 2;
            this.cropY = centerY - this.cropSize / 2;
            
            // Constrain within bounds
            this.cropX = Math.max(this.offsetX, Math.min(this.cropX, this.offsetX + this.scaledWidth - this.cropSize));
            this.cropY = Math.max(this.offsetY, Math.min(this.cropY, this.offsetY + this.scaledHeight - this.cropSize));
            
            this.draw();
        }
    }
    
    draw() {
        // Clear canvas
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Draw image
        this.ctx.drawImage(
            this.image,
            this.offsetX, this.offsetY,
            this.scaledWidth, this.scaledHeight
        );
        
        // Draw overlay (darken areas outside crop)
        this.ctx.fillStyle = 'rgba(0, 0, 0, 0.5)';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        
        // Clear crop area
        this.ctx.clearRect(this.cropX, this.cropY, this.cropSize, this.cropSize);
        
        // Redraw image in crop area only
        this.ctx.drawImage(
            this.image,
            this.offsetX, this.offsetY,
            this.scaledWidth, this.scaledHeight
        );
        
        // Draw crop border
        this.ctx.strokeStyle = '#51c3b7';
        this.ctx.lineWidth = 2;
        this.ctx.strokeRect(this.cropX, this.cropY, this.cropSize, this.cropSize);
        
        // Draw corner handles
        this.drawCornerHandles();
    }
    
    drawCornerHandles() {
        const handleSize = 8;
        this.ctx.fillStyle = '#51c3b7';
        
        // Top-left
        this.ctx.fillRect(this.cropX - handleSize/2, this.cropY - handleSize/2, handleSize, handleSize);
        // Top-right
        this.ctx.fillRect(this.cropX + this.cropSize - handleSize/2, this.cropY - handleSize/2, handleSize, handleSize);
        // Bottom-left
        this.ctx.fillRect(this.cropX - handleSize/2, this.cropY + this.cropSize - handleSize/2, handleSize, handleSize);
        // Bottom-right
        this.ctx.fillRect(this.cropX + this.cropSize - handleSize/2, this.cropY + this.cropSize - handleSize/2, handleSize, handleSize);
    }
    
    getCroppedImage() {
        // Create a new canvas for the cropped image
        const croppedCanvas = document.createElement('canvas');
        croppedCanvas.width = this.options.width;
        croppedCanvas.height = this.options.height;
        const croppedCtx = croppedCanvas.getContext('2d');
        
        // Calculate source coordinates in original image
        const sourceX = (this.cropX - this.offsetX) / this.scale;
        const sourceY = (this.cropY - this.offsetY) / this.scale;
        const sourceSize = this.cropSize / this.scale;
        
        // Draw cropped area to new canvas
        croppedCtx.drawImage(
            this.image,
            sourceX, sourceY, sourceSize, sourceSize,
            0, 0, this.options.width, this.options.height
        );
        
        return croppedCanvas.toDataURL('image/jpeg', this.options.quality);
    }
}
