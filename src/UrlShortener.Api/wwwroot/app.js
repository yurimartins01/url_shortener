const longUrlField = document.getElementById("long-url")
const shortUrlField = document.getElementById("short-url")
const btnShorten = document.getElementById("btn-shorten")
const errorField = document.getElementById("error")
const outputContainer = document.querySelector(".output-container")
const btnCopy = document.getElementById("btn-copy")
let urlGenerated = false


btnShorten.addEventListener("click", async function () {
    if (!urlGenerated) {
        disableShortenButton()
        errorField.hidden = true
        const result = await generateShortUrl()
        if (!result.success) {
            btnShorten.innerText = "Gerar link curto"
            showErrorMessage(result.error)
            return 
        }

        shortUrlField.value = result.data.shortUrl
        setConfigurationToShortLink()
    }
    else {
        setConfigurationToDefault()
    }
})

btnCopy.addEventListener("click", function () {
    copyShortUrl()
})

async function generateShortUrl() {
    try {

        const response = await fetch("api/short-url", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ originalUrl: longUrlField.value }),
        })
        if (!response.ok) {
            let errorMessage = await response.text()
 
            return {
                success: false,
                error: errorMessage.slice(1, -1)
            };
        }

        return {
            success: true,
            data: await response.json()
        };
    }   
    catch (e) {
        console.error("Erro ao criar url curta: ", e.message)

        return {
            success: false,
            error: "Não foi possível conectar ao servidor."
        };
    }
    finally {
        btnShorten.disabled = false
    }
    
}

async function copyShortUrl() {

    try {
        await navigator.clipboard.writeText(shortUrlField.value);
    }
    catch (e) {
        console.error("Erro ao copiar: ", e.message)
    }
}

function setConfigurationToDefault() {
    urlGenerated = false
    btnShorten.innerText = "Gerar link curto"
    outputContainer.hidden = true
    longUrlField.value = ""
    longUrlField.readOnly = false
    shortUrlField.value = ""
}

function setConfigurationToShortLink() {
    urlGenerated = true
    btnShorten.innerText = "Gerar novo link curto"
    longUrlField.readOnly = true
    outputContainer.hidden = false
}

function disableShortenButton() {

    btnShorten.disabled = true
    btnShorten.innerText = "Gerando link"
}

function showErrorMessage(error) {
    errorField.textContent = error
    errorField.hidden = false

}