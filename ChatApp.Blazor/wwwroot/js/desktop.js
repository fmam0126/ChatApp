window.DesktopInterop = {
    makeDraggable: function (element, titlebar, dotnetRef) {
        let offsetX = 0, offsetY = 0;
        let isDragging = false;

        titlebar.addEventListener('mousedown', function (e) {
            if (e.target.tagName === 'BUTTON') return;

            isDragging = true;
            var rect = element.getBoundingClientRect();
            offsetX = e.clientX - rect.left;
            offsetY = e.clientY - rect.top;
            element.style.cursor = 'move';
            e.preventDefault();
        });

        document.addEventListener('mousemove', function (e) {
            if (!isDragging) return;

            var left = e.clientX - offsetX;
            var top = e.clientY - offsetY;

            left = Math.max(0, Math.min(left, window.innerWidth - 100));
            top = Math.max(0, Math.min(top, window.innerHeight - 60));

            element.style.left = left + 'px';
            element.style.top = top + 'px';
        });

        document.addEventListener('mouseup', function (e) {
            if (!isDragging) return;

            isDragging = false;
            element.style.cursor = '';

            var rect = element.getBoundingClientRect();
            var left = Math.round(rect.left);
            var top = Math.round(rect.top);

            if (!isNaN(left) && !isNaN(top)) {
                dotnetRef.invokeMethodAsync('OnDragEnd', left, top);
            }
        });
    }
};
