const { chromium, firefox } = require('playwright');

async function testPlaywright() {
    console.log('Testing Playwright with Chromium and Firefox...\n');
    
    // Test Chromium
    try {
        console.log('🔍 Testing Chromium...');
        const browserChromium = await chromium.launch({ headless: true });
        const pageChromium = await browserChromium.newPage();
        await pageChromium.goto('https://playwright.dev');
        const titleChromium = await pageChromium.title();
        console.log('✅ Chromium test passed! Page title:', titleChromium);
        await browserChromium.close();
    } catch (error) {
        console.log('❌ Chromium test failed:', error.message);
    }

    // Test Firefox
    try {
        console.log('\n🔍 Testing Firefox...');
        const browserFirefox = await firefox.launch({ headless: true });
        const pageFirefox = await browserFirefox.newPage();
        await pageFirefox.goto('https://playwright.dev');
        const titleFirefox = await pageFirefox.title();
        console.log('✅ Firefox test passed! Page title:', titleFirefox);
        await browserFirefox.close();
    } catch (error) {
        console.log('❌ Firefox test failed:', error.message);
    }

    console.log('\n🎉 Playwright installation test completed!');
}

testPlaywright();
