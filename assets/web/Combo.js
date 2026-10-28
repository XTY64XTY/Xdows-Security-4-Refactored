const combo = document.querySelector('.XU-Combo');
const trigger = combo.querySelector('.combo-button');
const popup = combo.querySelector('.combo-popup');
const items = combo.querySelectorAll('.combo-item');
const comboText = trigger.querySelector('.combo-text'); // 获取文本容器

const customColorInput = document.getElementById('CustomColorMode');
const saveCustomColorButton = document.getElementById('SaveCustomColorMode');

if (customColorInput && saveCustomColorButton) {
    customColorInput.remove();
    saveCustomColorButton.remove();
}

// 切换下拉状态
trigger.addEventListener('click', () => {
    const isExpanded = trigger.getAttribute('aria-expanded') === 'true';
    trigger.setAttribute('aria-expanded', !isExpanded);
    popup.setAttribute('data-visible', !isExpanded);
});

// 选项选择
items.forEach(item => {
    item.addEventListener('click', () => {
        items.forEach(i => i.setAttribute('aria-selected', 'false'));
        item.setAttribute('aria-selected', 'true');
        comboText.textContent = item.textContent; // 更新文本容器内容
        popup.setAttribute('data-visible', 'false');
        trigger.setAttribute('aria-expanded', 'false');
    });
});

// 外部点击关闭
document.addEventListener('click', (e) => {
    if (!combo.contains(e.target)) {
        popup.setAttribute('data-visible', 'false');
        trigger.setAttribute('aria-expanded', 'false');
    }
});

// 键盘支持
trigger.addEventListener('keydown', (e) => {
    if (e.key === 'Enter' || e.key === ' ') {
        e.preventDefault();
        trigger.click();
    }
});