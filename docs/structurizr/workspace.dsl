workspace "Bookstore" "Institutional ordering architecture" {

    !identifiers hierarchical

    model {
        user = person "Institution User" "Places institutional orders and tracks their status."
        approver = person "Approver" "Approves or rejects orders; a purchasing manager is also an approver."
        portalAdmin = person "Portal Admin" "Manages budget categories and unit budgets."

        distributor = softwareSystem "Book Distributor" "Acme Book Distribution: receives and delivers approved orders." "External"
        mail = softwareSystem "E-mail Server" "SMTP relay for notification e-mails." "External"

        bookstore = softwareSystem "Bookstore" "Online bookstore; this model covers institutional ordering." {
            web = container "Web App" "Single-page app for institution users, approvers and portal admins." "Angular 22" "Browser"
            api = container "Bookstore.Api" "Institutional, Billing and Notifications modules (modular monolith)." "ASP.NET Core, .NET 10"
            db = container "Bookstore Database" "Orders, approvals, budgets." "SQL Server" "Database"
            cache = container "Cache" "Discount agreements and lookup data." "Redis" "Database"
            broker = container "Message Broker" "order-events topic: OrderApproved etc." "Azure Service Bus" "Queue"
            worker = container "Bookstore.Worker" "Sends notifications and approval reminders." ".NET 10 worker"
        }

        user -> bookstore.web "Creates and submits orders" "HTTPS"
        approver -> bookstore.web "Approves or rejects orders" "HTTPS"
        portalAdmin -> bookstore.web "Defines budget categories and budgets" "HTTPS"
        bookstore.web -> bookstore.api "Calls" "JSON/HTTPS"
        bookstore.api -> bookstore.db "Reads from and writes to" "EF Core / TDS"
        bookstore.api -> bookstore.cache "Caches discount agreements in" "RESP"
        bookstore.api -> bookstore.broker "Publishes events to" "AMQP"
        bookstore.broker -> bookstore.worker "Delivers events to" "AMQP"
        bookstore.api -> distributor "Places and cancels approved orders" "HTTPS"
        bookstore.worker -> mail "Sends e-mails via" "SMTP"
    }

    views {
        systemContext bookstore "Context" {
            include *
            autolayout lr
        }

        container bookstore "Containers" {
            include *
            autolayout lr
        }

        styles {
            element "Element" {
                color #ffffff
            }
            element "Person" {
                shape person
                background #08427b
            }
            element "Software System" {
                background #1168bd
            }
            element "Container" {
                background #438dd5
            }
            element "External" {
                background #999999
            }
            element "Database" {
                shape cylinder
            }
            element "Queue" {
                shape pipe
            }
            element "Browser" {
                shape webbrowser
            }
        }
    }
}
