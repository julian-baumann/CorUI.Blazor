export function open(backdrop, dialog) {
    backdrop.getBoundingClientRect();

    backdrop.animate([
        {
            opacity: 0
        },
        {
            opacity: 1
        }
    ], {
        duration: 300,
        easing: "ease-in-out",
        fill: "forwards"
    });

    dialog.animate([
        {
            transform: 'scale(0.8) translateZ(0)',
            opacity: 0
        },
        {
            opacity: 1,
            offset: 0.4
        },
        {
            transform: 'scale(1.02) translateZ(0)',
            offset: 0.8
        },
        {
            transform: 'scale(1) translateZ(0)',
            opacity: 1
        }
    ], {
        duration: 370,
        easing: 'cubic-bezier(0.25, 0.1, 0.25, 1)',
        fill: 'forwards'
    });
}