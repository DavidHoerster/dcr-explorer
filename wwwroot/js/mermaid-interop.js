window.dcrMermaid = (() => {
    let initialized = false;

    function ensureInitialized() {
        if (initialized || !window.mermaid) {
            return;
        }

        window.mermaid.initialize({
            startOnLoad: false,
            securityLevel: "loose",
            theme: "default",
            flowchart: {
                useMaxWidth: true,
                htmlLabels: true,
                curve: "basis"
            }
        });

        initialized = true;
    }

    return {
        async render(hostId, definition) {
            ensureInitialized();

            const host = document.getElementById(hostId);
            if (!host) {
                return;
            }

            const renderId = `${hostId}_svg_${Date.now()}`;
            const { svg } = await window.mermaid.render(renderId, definition);
            host.innerHTML = svg;
        },
        dispose(hostId) {
            const host = document.getElementById(hostId);
            if (host) {
                host.innerHTML = "";
            }
        }
    };
})();
