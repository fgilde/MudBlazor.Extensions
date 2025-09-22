import { createChevronRightButton } from '../../../svg';
export function createDropdownElementHandle() {
    const el = document.createElement('div');
    el.className = 'dv-tabs-overflow-dropdown-default';
    const text = document.createElement('span');
    text.textContent = ``;
    const icon = createChevronRightButton();
    el.appendChild(icon);
    el.appendChild(text);
    return {
        element: el,
        update: (params) => {
            text.textContent = `${params.tabs}`;
        },
    };
}
