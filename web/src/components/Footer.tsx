import { Flex, Button } from "@stardust-ui/react";
import { Common } from "../locales";
import { Link } from "react-router-dom";
import React from "react";
import { useTranslation } from "react-i18next";

const Footer: React.FC = () => {
    const { t } = useTranslation();
    return (
        <Flex as="footer" vAlign="center" hAlign="center">
            <Button
                as="a"
                href="https://github.com/NewFuture/custom-stickers-teams-extension/issues/new"
                text
                secondary
                size="smallest"
                target="_blank"
                rel="noopener noreferrer"
                color="info"
            >
                {t(Common.feedback)}
            </Button>
            <Button
                as="a"
                href="https://github.com/NewFuture/custom-stickers-teams-extension"
                text
                secondary
                size="smallest"
                target="_blank"
                rel="noopener noreferrer"
                color="info"
            >
                Github
            </Button>
            <Button as={Link} text secondary to="/privacy.html" size="smallest" color="info">
                {t(Common.privacyTitle)}
            </Button>
            <Button as={Link} text secondary to="/terms.html" size="smallest" color="info">
                {t(Common.termsTitle)}
            </Button>
        </Flex>
    );
};

export default Footer;
