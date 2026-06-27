window.dcrMermaid = (() => {
    let initialized = false;

    function ensureInitialized() {
        if (initialized || !window.mermaid) {
            return;
        }

        window.mermaid.initialize({
            startOnLoad: false,
            // strict: Mermaid HTML-escapes label text and forbids embedded markup
            // and click handlers. Combined with htmlLabels:false this neutralizes
            // attacker-influenced Azure resource names reaching the diagram source.
            securityLevel: "strict",
            theme: "default",
            flowchart: {
                useMaxWidth: true,
                htmlLabels: false,
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
            // mermaid.render returns SVG already sanitized by Mermaid under
            // securityLevel:"strict". This sanitized SVG is the ONLY value ever
            // assigned to innerHTML; raw/untrusted text is never injected here.
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
