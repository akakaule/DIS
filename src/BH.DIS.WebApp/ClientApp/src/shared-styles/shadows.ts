import { css, SerializedStyles } from '@emotion/react';

const shadowStyles = {
    shadow1: createShadow("0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24)"),
    shadow2: createShadow("0 3px 6px rgba(0,0,0,0.16), 0 3px 6px rgba(0,0,0,0.23)"),
    shadow3: createShadow("0 10px 20px rgba(0,0,0,0.19), 0 6px 6px rgba(0,0,0,0.23)"),
    shadow4: createShadow("0 14px 28px rgba(0,0,0,0.25), 0 10px 10px rgba(0,0,0,0.22)"),
    shadow5: createShadow("0 19px 38px rgba(0,0,0,0.30), 0 15px 12px rgba(0,0,0,0.22)"),
    hoverShadow1: createHoverShadow("0 1px 3px rgba(0,0,0,0.12), 0 1px 2px rgba(0,0,0,0.24)"),
    hoverShadow2: createHoverShadow("0 3px 6px rgba(0,0,0,0.16), 0 3px 6px rgba(0,0,0,0.23)"),
    hoverShadow3: createHoverShadow("0 10px 20px rgba(0,0,0,0.19), 0 6px 6px rgba(0,0,0,0.23)"),
    hoverShadow4: createHoverShadow("0 14px 28px rgba(0,0,0,0.25), 0 10px 10px rgba(0,0,0,0.22)"),
    hoverShadow5: createHoverShadow("0 19px 38px rgba(0,0,0,0.30), 0 15px 12px rgba(0,0,0,0.22)"),
}

export default shadowStyles;

function createShadow(shadow: string): SerializedStyles {
    return css({
        boxShadow: shadow
    })
}

function createHoverShadow(shadow: string): SerializedStyles {
    return css({
        ":hover": { ...createShadow(shadow) },
        transition: "box-shadow 0.3s ease-in-out"
    })
}